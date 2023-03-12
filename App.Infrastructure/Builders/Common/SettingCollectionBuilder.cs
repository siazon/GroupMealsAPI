using App.Domain.Common.Setting;
using System;
using System.Collections.Generic;
using App.Domain.Enum;

namespace App.Infrastructure.Builders.Common
{
    public class SettingCollectionBuilder
    {
        private readonly List<DbSetting> _collection;

        public SettingCollectionBuilder()
        {
            _collection = new List<DbSetting>();
        }

        public SettingCollectionBuilder GeneralInfo()
        {
            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppSmtpenabled,
                SettingValue = "true",
                IsServer = true
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppEmailApiKey,
                SettingValue = "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",
                IsServer = true
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppAllowEmailClient,
                SettingValue = "true",
                IsServer = true
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppAllowEmailShop,
                SettingValue = "true",
                IsServer = true
            });
            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppPublicKey,
                SettingValue = "pk_live_EB1I1zuNZZBraFKlxUaQUpIr",
                IsServer = true
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppStripKey,
                SettingValue = "sk_test_666n3dIJQIHoqChmChkAar5L",
                IsServer = true
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppPrinterCashTillCommand,
                SettingValue = "27, 112, 48, 55, 121",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = ServerSettingEnum.AppUserTranslateInternal,
                SettingValue = "0",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrintName}1",
                SettingValue = "GP-L80250 Series",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterReceiptWidth}1",
                SettingValue = "280",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}1",
                SettingValue = "1",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}1",
                SettingValue = "5",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterFontName}1",
                SettingValue = "Arial",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterLargeFontSize}1",
                SettingValue = "14",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterMediumFontSize}1",
                SettingValue = "12",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterSmallFontSize}1",
                SettingValue = "7",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterPrintLanguage}1",
                SettingValue = "0",
                IsServer = false
            });

            //2
            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrintName}2",
                SettingValue = "",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterReceiptWidth}2",
                SettingValue = "280",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}2",
                SettingValue = "1",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}2",
                SettingValue = "5",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterFontName}2",
                SettingValue = "Arial",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterLargeFontSize}2",
                SettingValue = "14",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterMediumFontSize}2",
                SettingValue = "12",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterSmallFontSize}2",
                SettingValue = "7",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterPrintLanguage}2",
                SettingValue = "0",
                IsServer = false
            });


            //3

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrintName}3",
                SettingValue = "",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterReceiptWidth}3",
                SettingValue = "280",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}3",
                SettingValue = "1",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}3",
                SettingValue = "5",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterFontName}3",
                SettingValue = "Arial",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterLargeFontSize}3",
                SettingValue = "14",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterMediumFontSize}3",
                SettingValue = "12",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterSmallFontSize}3",
                SettingValue = "7",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterPrintLanguage}3",
                SettingValue = "0",
                IsServer = false
            });


            //4
            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrintName}4",
                SettingValue = "",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterReceiptWidth}4",
                SettingValue = "280",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}4",
                SettingValue = "1",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}4",
                SettingValue = "5",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterFontName}4",
                SettingValue = "Arial",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterLargeFontSize}4",
                SettingValue = "14",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterMediumFontSize}4",
                SettingValue = "12",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterSmallFontSize}4",
                SettingValue = "7",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterPrintLanguage}4",
                SettingValue = "0",
                IsServer = false
            });


            //5
            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrintName}5",
                SettingValue = "",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterReceiptWidth}5",
                SettingValue = "280",
                IsServer = false
            });


            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}5",
                SettingValue = "1",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterXflow}5",
                SettingValue = "5",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterFontName}5",
                SettingValue = "Arial",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterLargeFontSize}5",
                SettingValue = "14",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterMediumFontSize}5",
                SettingValue = "12",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterSmallFontSize}5",
                SettingValue = "7",
                IsServer = false
            });

            _collection.Add(new DbSetting()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SettingKey = $"{ServerSettingEnum.AppPrinterPrintLanguage}5",
                SettingValue = "0",
                IsServer = false
            });

            return this;
        }


        public SettingCollectionBuilder Food()
        {
            return this;
        }

        public SettingCollectionBuilder IE()
        {
            return this;
        }

        public List<DbSetting> Build()
        {
            return _collection;
        }

        public SettingCollectionBuilder UK()
        {
            return this;
        }
        public SettingCollectionBuilder PL()
        {
            return this;
        }
    }
}