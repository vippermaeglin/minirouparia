﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.CoreModule.Data.Shipping
{
    public class ShippifyShippingMethod : ShippingMethod
    {
        public ShippifyShippingMethod()
            : base("Shippify")
        {
        }

        public ShippifyShippingMethod(params SettingEntry[] settings)
            : base("Shippify")
        {
            Settings = settings;
        }

        private decimal Rate
        {
            get
            {
                decimal retVal = 0;

                //TODO: calculate rates using pagseguro
                var settingRate = Settings.Where(x => x.Name == "VirtoCommerce.Core.FixedRateShippingMethod.Rate").FirstOrDefault();
                if (settingRate != null)
                {
                    retVal = Decimal.Parse(settingRate.Value, CultureInfo.InvariantCulture);
                }
                return 15.0m;// retVal;
            }
        }

        public override IEnumerable<ShippingRate> CalculateRates(Domain.Common.IEvaluationContext context)
        {
            var shippingEvalContext = context as ShippingEvaluationContext;
            if (shippingEvalContext == null)
            {
                throw new NullReferenceException("shippingEvalContext");
            }
            return new ShippingRate[] { new ShippingRate { Rate = Rate, Currency = shippingEvalContext.ShoppingCart.Currency, ShippingMethod = this } };
        }
    }
}