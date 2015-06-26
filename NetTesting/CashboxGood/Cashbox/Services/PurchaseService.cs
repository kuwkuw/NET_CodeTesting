﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cashbox.DataAccess;
using Cashbox.Models;
using Cashbox.ViewModels;

namespace Cashbox.Services
{
    internal class PurchaseService : IPurchaseService
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;

        public PurchaseService(IUnitOfWorkFactory unitOfWorkFactory)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
        }

        public decimal GetOrdersHistoryTotal(int accountId)
        {
            using (var uow = _unitOfWorkFactory.Create())
            {
                return uow.Repository<Order>()
                          .Query()
                          .Where(x => x.AccountId == accountId)
                          .Sum(x => (decimal?)x.Total) ?? 0;
            }
        }

        public decimal GetProductsTotal(IEnumerable<ProductViewModel> products)
        {
            return products.Sum(x => x.Price);
        }

        public decimal GetProductsTotal(IEnumerable<Product> products)
        {
            return products.Sum(x => x.Price);
        }

        public decimal GetDiscount(int accountId, decimal productsTotal)
        {
            var discount = 0.0m;

            // If account has orders history with sum total >= 500 then give 10% discount.
            var ordersHistoryTotal = GetOrdersHistoryTotal(accountId);
            if (ordersHistoryTotal >= 500)
            {
                discount += 0.1m;
            }

            // If the new order has total >= 200 then give an additional 5% discount.
            if (productsTotal >= 200)
            {
                discount += 0.05m;
            }

            return discount;
        }

        public decimal GetTotalAfterDiscount(decimal total, decimal discount)
        {
            return Math.Round(total * (1 - discount), 2);
        }

        public void Purchase(int accountId, IEnumerable<ProductViewModel> selectedProducts)
        {
            using (var uow = _unitOfWorkFactory.Create())
            {
                var productIds = selectedProducts.Select(x => x.Id);
                var products = uow.Repository<Product>().Query().Where(x => productIds.Contains(x.Id)).ToList();

                var total = GetProductsTotal(products);
                var discount = GetDiscount(accountId, total);
                var totalAfterDiscount = GetTotalAfterDiscount(total, discount);

                var account = uow.Repository<Account>().Get(accountId);

                if (totalAfterDiscount > account.Balance)
                    throw new PurchaseException(PurchaseError.NotEnoughMoney, "Not enough money!");

                uow.Repository<Order>().Add(new Order { Account = account, Date = DateTime.Now, Total = total });
                account.Balance -= total;
                
                foreach (var product in products)
                {
                    product.Amount--;
                }

                uow.SaveChanges();
            }
        }
    }
}