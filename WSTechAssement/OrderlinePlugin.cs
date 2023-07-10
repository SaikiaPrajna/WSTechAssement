using CrmEarlyBound;
using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace WSTechAssement
{
    //This plugin is used to calculate order line subtotal,total and totaldisocunt 
    //this will be triggered on saving the order line
    //here we are using early bound class concept
    public class OrderlinePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            //get trace service

            ITracingService tracingService = (ITracingService)serviceProvider.GetService(
 typeof(ITracingService));

            //get plugin context
            
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(
 typeof(IPluginExecutionContext));

            //context check for entity

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity && context.Stage == 20)
            {
                //get organisation  service

                IOrganizationServiceFactory serviceFactory =
 (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    Entity targetEntity = (Entity)context.InputParameters["Target"];
                    //check if the entity is ws_orderline
                    if (targetEntity != null && targetEntity.LogicalName.ToLower() == "ws_orderline")
                    {
                        ws_OrderLine ws_orderline = targetEntity.ToEntity<ws_OrderLine>();

                        //get user given Price, Quantity and discount percentage
                        Money price = ws_orderline.ws_Price;
                        decimal quantity = ws_orderline.ws_Quantity.Value;
                        decimal discountPerc = ws_orderline.ws_DiscountPercentage.Value;

                        //calculations for subtotal, totalDiscount and total
                        Money subtotal = new Money(price.Value * quantity);
                        Money totalDiscount = new Money(subtotal.Value * (discountPerc / 100));
                        Money total = new Money(subtotal.Value - totalDiscount.Value);

                        ws_orderline.ws_Subtotal = subtotal;
                        ws_orderline.ws_TotalDiscount = totalDiscount;
                        ws_orderline.ws_Total = total;
                        
                        //update the entity
                        service.Update(ws_orderline);
                    }
                }
                catch (Exception ex)
                {
                    tracingService.Trace("OrderLinePlugin has thrown an exception: " +  ex.Message);
                    throw new InvalidPluginExecutionException("An error occured in OrderLine Plugin: " + ex.Message);
                }
            }
        }
    }
}
