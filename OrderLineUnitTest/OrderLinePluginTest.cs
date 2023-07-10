using Microsoft.VisualStudio.TestTools.UnitTesting;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using WSTechAssement;
using System;
using CrmEarlyBound;

namespace OrderLineUnitTest
{
    [TestClass]
    public class OrderLinePluginTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            //Create Target Entity
            var targetId = Guid.NewGuid();
            var targetEntity = new ws_OrderLine()
            {
                Id = targetId
            };

            //Create input parameter
            var inputParameter = new ParameterCollection
            {
                { "Target", targetEntity }
            };

            //create plugin executoin context
            var fakePluginExecutionContext = new XrmFakedPluginExecutionContext()
            {
                MessageName = "Create",
                Stage = 20,
                UserId = Guid.NewGuid(),
                PrimaryEntityName = "ws_orderline",
                PrimaryEntityId = targetId,
                InputParameters = inputParameter
            };

            //create orderline dummy record
            targetEntity.ws_Price = new Money(200);
            targetEntity.ws_Quantity = 20;
            targetEntity.ws_DiscountPercentage = 10;

            service.Create(targetEntity);

            context.ExecutePluginWith<OrderlinePlugin>(fakePluginExecutionContext);
            Assert.AreEqual(targetEntity.ws_Subtotal, new Money(4000));
            Assert.AreEqual(targetEntity.ws_TotalDiscount, new Money(400));
            Assert.AreEqual(targetEntity.ws_Total, new Money(3600));
            
        }
    }
}
