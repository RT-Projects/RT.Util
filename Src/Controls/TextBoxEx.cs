using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.ExtensionMethods;
using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    ///     Provides a textbox with extra functionality (see remarks).</summary>
    /// <remarks>
    ///     <para>Extra functionality currently supported:</para>
    ///     <list type="bullet">
    ///         <item><description>Ctrl+A selects all text.</description></item>
    ///     </list></remarks>
    public class TextBoxEx : TextBox
    {
        /// <summary>Override; see base.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.A))
            {
                SelectAll();
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
