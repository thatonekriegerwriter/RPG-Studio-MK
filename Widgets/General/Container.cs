﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKEditor.Widgets
{
    public class Container : Widget
    {
        public Container(IContainer Parent, int Index = -1) : base(Parent, Index)
        {

        }
    }
}
