using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Drawing.Drawing2D;

namespace RT.Controls
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint | 
                ControlStyles.DoubleBuffer, true
            );
        }
    }
}
