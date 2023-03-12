using App.Domain.Common.Shop;
using FluentValidation;

namespace App.Infrastructure.Validation
{
    public class ShopValidator : AbstractValidator<DbShop>, IValidator<DbShop>
    {
        public ShopValidator()
        {
            RuleFor(reg => reg.ShopName).NotNull();
            RuleFor(reg => reg.ShopName).NotEmpty();
            RuleFor(reg => reg.ShopNumber).NotEmpty();
            RuleFor(reg => reg.ShopWeChat).NotEmpty();
            RuleFor(reg => reg.ShopWeChatQRCode).NotEmpty();
            RuleFor(reg => reg.ContactEmail).NotEmpty();
            RuleFor(reg => reg.ShopOpenHours).NotEmpty();
            RuleFor(reg => reg.CountryId).GreaterThan(0);
        }
    }
}