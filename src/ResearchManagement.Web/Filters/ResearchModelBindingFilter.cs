// Filters/ResearchModelBindingFilter.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ResearchManagement.Web.Models.ViewModels.Research;

namespace ResearchManagement.Web.Filters
{
    public class ResearchModelBindingFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("model", out var modelObj) &&
                modelObj is CreateResearchViewModel model)
            {
                // تنظيف Model State من الأخطاء المتعلقة بالقوائم المنسدلة
                var keysToRemove = new List<string>();
                foreach (var key in context.ModelState.Keys)
                {
                    if (key.Contains("Options") ||
                        key.Contains("ResearchTypeOptions") ||
                        key.Contains("LanguageOptions") ||
                        key.Contains("TrackOptions"))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    context.ModelState.Remove(key);
                }

                // إصلاح checkbox values للمؤلفين
                if (model.Authors != null)
                {
                    var request = context.HttpContext.Request;

                    for (int i = 0; i < model.Authors.Count; i++)
                    {
                        var checkboxKey = $"Authors[{i}].IsCorresponding";

                        // التحقق من القيمة المرسلة
                        if (request.Form.ContainsKey(checkboxKey))
                        {
                            var values = request.Form[checkboxKey].ToArray();

                            // إذا كان checkbox محدد، ستكون القيم ["true", "false"] أو ["on", "false"]
                            // إذا لم يكن محدد، ستكون القيمة ["false"] فقط
                            model.Authors[i].IsCorresponding = values.Contains("true") || values.Contains("on");
                        }
                        else
                        {
                            model.Authors[i].IsCorresponding = false;
                        }

                        // إزالة الأخطاء المتعلقة بـ IsCorresponding
                        var errorKey = $"Authors[{i}].IsCorresponding";
                        if (context.ModelState.ContainsKey(errorKey))
                        {
                            context.ModelState.Remove(errorKey);
                        }
                    }
                }

                // تنظيف الأخطاء الفارغة
                var emptyErrorKeys = context.ModelState
                    .Where(x => !x.Value.Errors.Any() || x.Value.Errors.All(e => string.IsNullOrEmpty(e.ErrorMessage)))
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in emptyErrorKeys)
                {
                    context.ModelState.Remove(key);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}