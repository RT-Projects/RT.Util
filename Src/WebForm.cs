using System;
using System.Linq.Expressions;
using System.Reflection;
using RT.Servers;
using RT.TagSoup.HtmlTags;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.KitchenSink
{
    public abstract class WebForm
    {
        public static T FromRequest<T>(HttpRequest req) where T : WebForm, new()
        {
            return (T) FromRequest(typeof(T), req);
        }

        public static WebForm FromRequest(Type webFormType, HttpRequest req)
        {
            if (webFormType == null)
                throw new ArgumentNullException("webFormType");
            if (!typeof(WebForm).IsAssignableFrom(webFormType))
                throw new ArgumentException("Type must derive from {0}.".Fmt(typeof(WebForm).FullName), "webFormType");

            var ret = (WebForm) Activator.CreateInstance(webFormType);
            ret.BeforeFromRequest(req);
            foreach (var f in webFormType.GetAllFields())
            {
                if (!f.IsDefined<FormFieldAttribute>())
                    continue;

                if (ExactConvert.IsSupportedType(f.FieldType))
                    f.SetValue(ret, ExactConvert.To(f.FieldType, req.Post[f.Name].Value));
                else
                    throw new InvalidOperationException("Type {0} not supported.".Fmt(f.FieldType));
            }
            ret.AfterFromRequest(req);
            return ret;
        }

        protected virtual void BeforeFromRequest(HttpRequest req) { }
        protected virtual void AfterFromRequest(HttpRequest req) { }
        public abstract object GetHtml(HttpRequest req);

        protected object textbox(Expression<Func<string>> fieldExpr, string error = null, int size = 60, bool password = false)
        {
            var expr = fieldExpr.Body as MemberExpression;
            if (expr == null)
                throw new InvalidOperationException("Expression must be a field access expression.");
            var field = expr.Member as FieldInfo;
            if (field == null)
                throw new InvalidOperationException("Expression must be a field access expression.");
            var input = new INPUT { type = password ? itype.password : itype.text, name = field.Name, value = (string) field.GetValue(this), size = size };
            if (error == null)
                return input;
            return new object[] { input, new DIV(error) { class_ = "error" } };
        }

        protected object hidden(Expression<Func<string>> fieldExpr)
        {
            var expr = fieldExpr.Body as MemberExpression;
            if (expr == null)
                throw new InvalidOperationException("Expression must be a field access expression.");
            var field = expr.Member as FieldInfo;
            if (field == null)
                throw new InvalidOperationException("Expression must be a field access expression.");
            var value = field.GetValue(this);
            if (value == null)
                return null;
            return new INPUT { type = itype.hidden, name = field.Name, value = (string) value };
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class FormFieldAttribute : Attribute { }
}
