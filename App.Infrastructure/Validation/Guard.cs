using FluentValidation;
using FluentValidation.Results;

namespace App.Infrastructure.Validation
{
    public static class Guard
    {
        public static void NotNull<T>(T value, string name = null) where T : class
        {
            if (value == null)
                throw new ValidationException(new[] { new ValidationFailure("Object", "Object Is Null") });
        }

        public static void GreaterThanZero(int value)
        {
            if (value <= 0)
                throw new ValidationException(new[] { new ValidationFailure("Integer", "Object Is Null") });
        }

        public static void AreEqual(int value1, int value2)
        {
            if (value1 != value2)
                throw new ValidationException(new[] { new ValidationFailure("Integer", "Value are not equal") });
        }

        public static void GreaterThan(int value, int amount)
        {
            if (value <= amount)
                throw new ValidationException(new[] { new ValidationFailure("Integer", "Value needs to be greater") });
        }
    }
}