using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WSTechAssement
{
    //This plugin is used to calculate order header total,subtotal,totaldiscount and totaldiscountpercentage
    //This plugin will be triggered on postoperation stage of creating a new orrderline record 
    //late bound concept is used here
    public class OrderHeaderPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity entity = (Entity)context.InputParameters["Target"];

                //check for entity and stage is post operation or not 
                if(entity.LogicalName.ToLower() == "ws_orderline" && context.Stage == 40)
                {
                    try
                    {
                        IOrganizationServiceFactory organizationService =
                            (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                        IOrganizationService service = (IOrganizationService)organizationService.CreateOrganizationService(context.UserId);

                        //get the related orderHead id and retrieve it
                        ColumnSet columnSet = new ColumnSet(true);
                        Guid orderHeaderGuid = entity.GetAttributeValue<EntityReference>("ws_orderheaderid").Id;
                        Entity orderHeaderEntity =  service.Retrieve("ws_orderheader", orderHeaderGuid, columnSet);

                        Entity orderHeaderEntityWithUpdateValues =  GetOrderHeaderWithUpdatedvalues(orderHeaderEntity, entity);
                       
                        service.Update(orderHeaderEntityWithUpdateValues);
                    }
                    catch (FaultException ex)
                    {
                        tracingService.Trace("OrderHeaderPlugin has thrown an exception: " + ex.Message);
                        throw new InvalidPluginExecutionException("An error occured in OrderHeaderPlugin: " + ex.Message);
                    }
                }

            }
        }

        private Entity GetOrderHeaderWithUpdatedvalues(Entity orderHeader, Entity orderLine)
        {
            //here we are getting old values of this related orderhead if any
            var subtotal = orderHeader.TryGetAttributeValue<Money>("ws_subtotal", out Money x) ? x.Value : 0;
            var discountpercentage = orderHeader.TryGetAttributeValue<int>("ws_discountpercentage", out int y) ? y : 0;
            var totaldiscount = orderHeader.TryGetAttributeValue<Money>("ws_totaldiscount", out Money a) ? a.Value : 0;
            var total = orderHeader.TryGetAttributeValue<decimal>("ws_total", out decimal b) ? b : 0;

            //get this new orderline values
            var olsubtotal = orderLine.GetAttributeValue<Money>("ws_subtotal").Value;
            var oltotaldiscount = orderLine.GetAttributeValue<Money>("ws_totaldiscount").Value;
            var olTotal = orderLine.GetAttributeValue<Money>("ws_total").Value;


            //logic to calculate orderHead entity values
            //here we are adding new values to exisitng values
            var orderHeaderTotalDiscount = totaldiscount + oltotaldiscount;

            orderHeader["ws_subtotal"] = new Money(subtotal + olsubtotal);
            orderHeader["ws_totaldiscount"] = new Money(orderHeaderTotalDiscount);
            orderHeader["ws_discountpercentage"] = decimal.ToInt32((orderHeaderTotalDiscount / (subtotal + olsubtotal)) * 100);
            orderHeader["ws_total"] = total + olTotal;

            return orderHeader;
        }
    }
}
