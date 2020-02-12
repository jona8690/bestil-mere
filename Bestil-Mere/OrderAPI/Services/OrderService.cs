using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Models;
using Models.Messages.Payment;
using Models.Order;
using Models.Payment;
using MongoDB.Driver;
using OrderAPI.Db;
using OrderAPI.Messaging;
using OrderAPI.Models;
using ExtraMealItem = OrderAPI.Models.ExtraMealItem;
using MealItem = OrderAPI.Models.MealItem;

namespace OrderAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly MessagePublisher _publisher;
        public OrderService(MongoDbManager mgr, MessagePublisher publisher)
        {
            _orders = mgr.Orders;
            _publisher = publisher;
        }

        public async Task<List<Order>> Get()
        {
            var findAll = await _orders.FindAsync(x => true);
            return await findAll.ToListAsync(); 
        }

        public async Task<Order> Get(string id)
        {
            var findAll = await _orders.FindAsync(order => order.Id == id);
            return await findAll.FirstOrDefaultAsync();

        }

        public async Task<Order> Create(CreateOrderModel createOrderModel)
        {
            var order = new Order
            {
                CustomerId = createOrderModel.CustomerId,
                RestaurantId = createOrderModel.RestaurantId,
                OrderLines = createOrderModel.OrderLines.Select(x => new OrderLine()
                {
                    Quantity = x.Quantity,
                    Meal = new Meal()
                    {
                        Name = x.Meal.Name,
                        MealItems = x.Meal.MealItems.Select(m => new MealItem() {Name = m.Name}),
                        ExtraMealItems = x.Meal.ExtraMealItems.Select(em => new ExtraMealItem()
                        {
                            Name = em.Name, Quantity = em.Quantity
                        })
                    }
                }),
                OrderStatus = OrderStatus.Created
            };
            // Right now we just insert and message our payment api, and then we return the order
            //
            // To make it simple, we should all this logic, messaging etc, and whenever the order is 
            // accepted/rejected by the res, return the order and make the client wait.
            await _orders.InsertOneAsync(order);
            _publisher.AuthorizePaymentForOrder(order);
            
            return order;
        }

        public void OnPaymentStatusUpdate(NewPaymentStatus status)
        {
            Console.WriteLine($"[New payment status] {status.Status.ToString()} on order: {status.OrderId}");
            if (status.Status != PaymentStatusDTO.Accepted) return;
            Thread.Sleep(5000); // Emulate Restaurant's response to this new Order
            Console.WriteLine($"[Payment Accepted] Proceeding with order..");
            ProceedOrder(status.OrderId);
        }

        private async void ProceedOrder(string statusOrderId)
        {
            var order = await Get(statusOrderId);
            order.OrderStatus = OrderStatus.Accepted;
            // Emit to client that his order has been ordered.
            
        }

        public void Update(string id, Order orderIn) =>
            _orders.ReplaceOne(order => order.Id == id, orderIn);

        public void Remove(Order orderIn) =>
            _orders.DeleteOne(order => order.Id == orderIn.Id);

        public void Remove(string id) => 
            _orders.DeleteOneAsync(order => order.Id == id);
    }
}