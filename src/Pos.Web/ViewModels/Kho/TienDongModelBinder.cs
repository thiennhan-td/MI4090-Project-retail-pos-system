using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Pos.Web.ViewModels.Kho;

public sealed class TienDongModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        if (string.IsNullOrEmpty(value.FirstValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
        }
        else if (decimal.TryParse(value.FirstValue, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
        {
            bindingContext.Result = ModelBindingResult.Success(amount);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                "Nhập số tiền nguyên (đồng), không dùng dấu phân cách.");
        }

        return Task.CompletedTask;
    }
}
