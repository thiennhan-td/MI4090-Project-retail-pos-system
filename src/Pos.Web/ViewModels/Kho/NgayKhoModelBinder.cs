using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Pos.Web.ViewModels.Kho;

public sealed class NgayKhoModelBinder : IModelBinder
{
    private static readonly string[] Formats = ["dd/MM/yyyy", "yyyy-MM-dd"];

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        if (string.IsNullOrWhiteSpace(value.FirstValue) && bindingContext.ModelMetadata.IsNullableValueType)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
        }
        else if (DateOnly.TryParseExact(value.FirstValue, Formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Nhập ngày hợp lệ theo định dạng dd/MM/yyyy.");
        }

        return Task.CompletedTask;
    }
}
