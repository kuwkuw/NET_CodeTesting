using System;
using System.Linq;
using Cashbox.DataAccess;
using Cashbox.Models;
using Cashbox.Services;
using Cashbox.Tests.Fake;
using FakeItEasy;
using NUnit.Framework;

namespace Cashbox.Tests.Services
{
    [TestFixture]
    public class PurchaseServiceTests
    {
        private readonly FakeUnitOfWorkFactory _fakeUnitOfWorkFactory;
        private readonly FakeRepository<Account> _fakeAccountRepository;
        private readonly FakeRepository<Order> _fakeOrderRepository;
        private readonly FakeRepository<Product> _fakeProductRepository;

        public PurchaseServiceTests()
        {
            _fakeAccountRepository = new FakeRepository<Account>();
            _fakeOrderRepository = new FakeRepository<Order>();
            _fakeProductRepository = new FakeRepository<Product>();

            _fakeUnitOfWorkFactory = new FakeUnitOfWorkFactory(
                uow =>
                {
                    uow.SetRepository(_fakeAccountRepository);
                    uow.SetRepository(_fakeOrderRepository);
                    uow.SetRepository(_fakeProductRepository);
                });
        }

        // This method runs before each test.
        [SetUp]
        public void TestSetup()
        {
            // Prepare here our fake dependencies.
            var account1 = new Account { Id = 1, Name = "Account1", Balance = 100.5m };
            var account2 = new Account { Id = 2, Name = "Account2", Balance = 2000.17m };

            var order1 = new Order
                         {
                             Id = 1,
                             AccountId = 1,
                             Account = account1,
                             Date = DateTime.Now,
                             Total = PurchaseService.ORDERS_HISTORY_DISCOUNT_THRESHOLD - 10
                         };
            var order2 = new Order { Id = 2, AccountId = 1, Account = account1, Date = DateTime.Now, Total = 20.1m };
            var order3 = new Order { Id = 3, AccountId = 2, Account = account2, Date = DateTime.Now, Total = 170.87m };

            var product1 = new Product { Id = 1, Title = "Product1", Price = 250.99m, Amount = 2 };
            var product2 = new Product { Id = 2, Title = "Product2", Price = 50.5m, Amount = 1 };
            var product3 = new Product { Id = 3, Title = "Product3", Price = 70.15m, Amount = 10 };
            var product4 = new Product { Id = 4, Title = "Product4", Price = 10.75m, Amount = 0 };

            _fakeAccountRepository.Data.AddRange(new[] { account1, account2 });
            _fakeOrderRepository.Data.AddRange(new[] { order1, order2, order3 });
            _fakeProductRepository.Data.AddRange(new[] { product1, product2, product3, product4 });
        }

        // This method runs after each test.
        [TearDown]
        public void TestTearDown()
        {
            // Clean up data in our fake dependencies.
            _fakeAccountRepository.Data.Clear();
            _fakeOrderRepository.Data.Clear();
            _fakeProductRepository.Data.Clear();
        }

        // Example with fake objects without using mock frameworks.
        [Test]
        public void GetDiscount_When_account_has_enough_orders_and_selected_cheap_product_Then_orders_history_discount()
        {
            // Arrange
            var service = new PurchaseService(_fakeUnitOfWorkFactory);

            // Act
            var discount = service.GetDiscount(1, 10.5m);

            // Assert
            Assert.That(discount, Is.EqualTo(PurchaseService.ORDERS_HISTORY_DISCOUNT));
        }

        // TODO 4: Write test to check that account can get 5% discount (for the selected expensive products). Fix code if test fails.
        [Test]
        public void GetDiscount_When_account_selected_expensiv_product_Then_account_hes_discount()
        {
            //Arreng
            var service = new PurchaseService(_fakeUnitOfWorkFactory);

            //Act
            var discount = service.GetDiscount(2, 200m);

            //Assert
            Assert.That(discount, Is.EqualTo(PurchaseService.EXPENSIVE_PRODUCTS_DISCOUNT));
        }
        // TODO 5: Write test to check that account can get 15% discount (10% + 5%, for previous orders and for selected products). Fix code if test fails.
        [Test]
        public void
            GetDiscount_When_account_has_order_history_for_discount_and_selected_expensive_product_Then_sum_discounts()
        {
            //Arreng
            var service = new PurchaseService(_fakeUnitOfWorkFactory);

            //Act
            var discount = service.GetDiscount(1, 200m);

            //Assert
            Assert.That(discount, Is.EqualTo(PurchaseService.EXPENSIVE_PRODUCTS_DISCOUNT+PurchaseService.ORDERS_HISTORY_DISCOUNT));
        }

        [Test]
        public void Purchase_When_not_enough_balance_Then_throw_exception()
        {
            // Assert
            var service = new PurchaseService(_fakeUnitOfWorkFactory);

            // Act and Assert
            Assert.Throws<PurchaseException>(() => service.Purchase(1, Enumerable.Empty<int>(), 200.50m));
        }

        // Example with fake objects created using the FakeItEasy.
        [Test]
        public void Purchase_When_purchase_products_Then_products_amount_correctly_updated()
        {
            // Arrange
            var product1 = new Product { Id = 1, Price = 1, Amount = 5 };
            var product2 = new Product { Id = 2, Price = 2, Amount = 10 };
            var product3 = new Product { Id = 3, Price = 3, Amount = 7 };

            var productRepository = A.Fake<IRepository<Product>>();
            A.CallTo(() => productRepository.Query()).Returns(new[] { product1, product2, product3 }.AsQueryable());

            var account = new Account { Id = 1, Balance = 10 };

            var accountRepository = A.Fake<IRepository<Account>>();
            A.CallTo(() => accountRepository.Get(A<int>._)).Returns(account);

            var unitOfWork = A.Fake<IUnitOfWork>();
            A.CallTo(() => unitOfWork.Repository<Product>()).Returns(productRepository);
            A.CallTo(() => unitOfWork.Repository<Account>()).Returns(accountRepository);

            var unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
            A.CallTo(() => unitOfWorkFactory.Create()).Returns(unitOfWork);

            var service = new PurchaseService(unitOfWorkFactory);

            // Act
            service.Purchase(account.Id, new[] { product1.Id, product2.Id }, product1.Price + product2.Price);

            // Assert
            Assert.That(product1.Amount, Is.EqualTo(4));
            Assert.That(product2.Amount, Is.EqualTo(9));
        }

        // Example of behavior test that checks that specific method was called.
        [Test]
        public void Purchase_When_purchase_products_Then_SaveChanges_of_UnitOfWork_called()
        {
            // Arrange
            var product1 = new Product { Id = 1, Price = 1, Amount = 1 };

            var productRepository = A.Fake<IRepository<Product>>();
            A.CallTo(() => productRepository.Query()).Returns(new[] { product1 }.AsQueryable());

            var account = new Account { Id = 1, Balance = 1 };

            var accountRepository = A.Fake<IRepository<Account>>();
            A.CallTo(() => accountRepository.Get(A<int>._)).Returns(account);

            var unitOfWork = A.Fake<IUnitOfWork>();
            A.CallTo(() => unitOfWork.Repository<Product>()).Returns(productRepository);
            A.CallTo(() => unitOfWork.Repository<Account>()).Returns(accountRepository);

            var unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
            A.CallTo(() => unitOfWorkFactory.Create()).Returns(unitOfWork);

            var service = new PurchaseService(unitOfWorkFactory);

            // Act
            service.Purchase(account.Id, new[] { product1.Id }, product1.Price);

            // Assert
            A.CallTo(() => unitOfWork.SaveChanges()).MustHaveHappened(Repeated.Exactly.Once);
        }

        // TODO 6: Write test to check that account balance is correctly updated after purchase. Fix code if test fails.
        [Test]
        public void Purchase_When_purchase_products_Then_acount_balance_correctly_updated()
        {
            // Arrange
            var product1 = new Product { Id = 1, Price = 100m, Amount = 1 };
            var product2 = new Product { Id = 2, Price = 50m, Amount = 1 };
            
            var productRepository = A.Fake<IRepository<Product>>();
            A.CallTo(() => productRepository.Query()).Returns(new[] { product1, product2}.AsQueryable());

            var account = new Account { Id = 1, Balance = 500m };

            var accountRepository = A.Fake<IRepository<Account>>();
            A.CallTo(() => accountRepository.Get(A<int>._)).Returns(account);

            var unitOfWork = A.Fake<IUnitOfWork>();
            A.CallTo(() => unitOfWork.Repository<Product>()).Returns(productRepository);
            A.CallTo(() => unitOfWork.Repository<Account>()).Returns(accountRepository);

            var unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
            A.CallTo(() => unitOfWorkFactory.Create()).Returns(unitOfWork);

            var service = new PurchaseService(unitOfWorkFactory);

            // Act
            service.Purchase(account.Id, new[] { product1.Id, product2.Id }, product1.Price + product2.Price);

            // Assert
            Assert.That(account.Balance, Is.EqualTo(350m));
            
        }
        // TODO 7: Write test to check that account can't buy product if it's amount is 0. Purchase should throw an exception. Fix code if test fails.
        [Test]
        [ExpectedException(typeof(PurchaseException))]
        public void Purchase_When_purchase_product_whit_amout_is_0_Then_throw_exception()
        {
            // Arrange
            var product = new Product { Id = 1, Price = 200, Amount = 0 };

            var productRepository = A.Fake<IRepository<Product>>();
            A.CallTo(() => productRepository.Query()).Returns(new[] { product}.AsQueryable());

            var account = new Account { Id = 1, Balance = 400 };

            var accountRepository = A.Fake<IRepository<Account>>();
            A.CallTo(() => accountRepository.Get(A<int>._)).Returns(account);

            var unitOfWork = A.Fake<IUnitOfWork>();
            A.CallTo(() => unitOfWork.Repository<Product>()).Returns(productRepository);
            A.CallTo(() => unitOfWork.Repository<Account>()).Returns(accountRepository);

            var unitOfWorkFactory = A.Fake<IUnitOfWorkFactory>();
            A.CallTo(() => unitOfWorkFactory.Create()).Returns(unitOfWork);

            var service = new PurchaseService(unitOfWorkFactory);

            // Act
            service.Purchase(account.Id, new[] { product.Id}, product.Price );

            // Assert
        }
    }
}