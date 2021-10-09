using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Xabbo.Scripter.Services
{
    public class ObjectFormatter : IObjectFormatter
    {
        private readonly PrintOptions _printOptions = new PrintOptions()
        {

        };

        public string FormatObject(object obj)
        {
            return CSharpObjectFormatter.Instance.FormatObject(obj, _printOptions);
        }

        public string FormatException(Exception e)
        {
            return CSharpObjectFormatter.Instance.FormatException(e);
        }
    }
}
