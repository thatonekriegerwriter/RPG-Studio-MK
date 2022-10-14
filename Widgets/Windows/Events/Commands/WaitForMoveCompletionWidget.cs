﻿using System;
using System.Collections.Generic;
using System.Linq;
using RPGStudioMK.Game;

namespace RPGStudioMK.Widgets.CommandWidgets;

public class WaitForMoveCompletionWidget : BaseCommandWidget
{
    public WaitForMoveCompletionWidget(IContainer Parent, int ParentWidgetIndex) : base(Parent, ParentWidgetIndex) { }

    public override void LoadCommand()
    {
        base.LoadCommand();
        HeaderLabel.SetText("Wait for Move's Completion");
    }

    protected override bool IsEditable()
    {
        return false;
    }
}
