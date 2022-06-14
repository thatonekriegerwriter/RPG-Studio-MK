﻿using System;
using System.Collections.Generic;
using RPGStudioMK.Game;
using RPGStudioMK.Widgets;
using System.Diagnostics;

namespace RPGStudioMK;

public partial class MainEditorWindow : UIWindow
{
    /// <summary>
    /// The main active mode.
    /// </summary>
    public Widget MainEditorWidget;

    /// <summary>
    /// The MappingWidget object of the mapping mode. Null if not active.
    /// </summary>
    public MappingWidget MapWidget { get { return MainEditorWidget as MappingWidget; } }
    /// <summary>
    /// The DatabaseWidget object of the database mode. Null if not active.
    /// </summary>
    public DatabaseWidget DatabaseWidget { get { return MainEditorWidget as DatabaseWidget; } }

    /// <summary>
    /// The main grid layout which divides menubar, toolbar, main area and statusbar from one another.
    /// </summary>
    public Grid MainGridLayout;
    /// <summary>
    /// The menubar.
    /// </summary>
    public MenuBar MenuBar;
    /// <summary>
    /// The status bar.
    /// </summary>
    public StatusBar StatusBar;
    /// <summary>
    /// The toolbar.
    /// </summary>
    public ToolBar ToolBar;
    /// <summary>
    /// The home screen, if shown.
    /// </summary>
    public HomeScreen HomeScreen;

    public MainEditorWindow(string ProjectFile) : base(false, false)
    {
        this.SetMinimumSize(675, 400);
        this.SetText("RPG Studio MK");

        this.Initialize();
        Windows.Add(this);
        OnClosed += _ => Windows.Remove(this);

        Editor.LoadGeneralSettings();
        SetPosition(Editor.GeneralSettings.LastX, Editor.GeneralSettings.LastY);
        SetSize(Editor.GeneralSettings.LastWidth, Editor.GeneralSettings.LastHeight);
        if (Editor.GeneralSettings.WasMaximized) Maximize();

        Action UpdateLastPosAndSize = () =>
        {
            bool Maximized = IsMaximized();
            if (!Maximized)
            {
                Point pos = GetPosition();
                Size size = GetSize();
                Editor.GeneralSettings.LastX = pos.X;
                Editor.GeneralSettings.LastY = pos.Y;
                Editor.GeneralSettings.LastWidth = size.Width;
                Editor.GeneralSettings.LastHeight = size.Height;
            }
            Editor.GeneralSettings.WasMaximized = Maximized;
        };
        this.OnClosing += delegate (BoolEventArgs e)
        {
            UpdateLastPosAndSize();
            Editor.DumpGeneralSettings();

            if (Editor.InProject)
            {
                // Save window when closing with the top-right X button
                if (Program.ReleaseMode && !Program.ThrownError)
                {
                    e.Value = true;
                    EnsureSaved(Dispose);
                }
                GameRunner.Stop();
            }
        };
        OnPositionChanged += _ => UpdateLastPosAndSize();
        OnSizeChanged += _ => UpdateLastPosAndSize();

        this.InitializeUI(10, 23, 37);
        UI.RegisterShortcut(new Shortcut(null, new Key(Keycode.Z, Keycode.CTRL), _ => Editor.Undo(), true));
        UI.RegisterShortcut(new Shortcut(null, new Key(Keycode.Y, Keycode.CTRL), _ => Editor.Redo(), true));

        // Widgets may now be created

        Editor.MainWindow = this;
        Utilities.Initialize();

        #region Grid
        MainGridLayout = new Grid(UI);
        MainGridLayout.SetSize(Width, Height);
        /* 0 m m m m m m m m m m m m m
         * 1 t t t t t t t t t t t t t
         * 2 - - - - - - - - - - - - -
         * 3 a a a a a a a a a a a a a
         *   a a a a a a a a a a a a a
         *   a a a a a a a a a a a a a
         *   a a a a a a a a a a a a a
         *   a a a a a a a a a a a a a
         * 4 - - - - - - - - - - - - -
         * 5 s s s s s s s s s s s s s
         * m => menubar
         * t => toolbar
         * a => main editor area (divided in a grid of its own)
         * s => statusbar
         * - => divider*/
        MainGridLayout.SetRows(
            new GridSize(32, Unit.Pixels),
            new GridSize(31, Unit.Pixels),
            new GridSize(1, Unit.Pixels),
            new GridSize(1),
            new GridSize(1, Unit.Pixels),
            new GridSize(26, Unit.Pixels)
        );

        #endregion
        #region Menubar + Toolbar
        Color DividerColor = new Color(79, 108, 159);

        // Header + Menubar
        MenuBar = new MenuBar(MainGridLayout);
        MenuBar.SetBackgroundColor(10, 23, 37);
        MenuBar.SetGridRow(0);
        MenuBar.SetItems(MenuBarItems);

        // Toolbar (modes, icons, etc)
        ToolBar = new ToolBar(MainGridLayout);
        ToolBar.SetBackgroundColor(28, 50, 73);
        ToolBar.SetGridRow(1);
        #endregion
        #region Dividers
        // Blue 1px separator
        Widget Blue1pxSeparator = new Widget(MainGridLayout);
        Blue1pxSeparator.SetBackgroundColor(DividerColor);
        Blue1pxSeparator.SetGridRow(2);

        // Status bar divider
        Widget StatusBarDivider = new Widget(MainGridLayout);
        StatusBarDivider.SetBackgroundColor(DividerColor);
        StatusBarDivider.SetGridRow(4);
        #endregion
        #region Statusbar
        // Status bar
        StatusBar = new StatusBar(MainGridLayout);
        StatusBar.SetGridRow(5);
        #endregion

        // If an argument was passed, load that project file and skip the home screen
        if (string.IsNullOrEmpty(ProjectFile))
        {
            MainGridLayout.Rows[1] = new GridSize(0, Unit.Pixels);
            MainGridLayout.Rows[4] = new GridSize(0, Unit.Pixels);
            MainGridLayout.Rows[5] = new GridSize(0, Unit.Pixels);
            MainGridLayout.UpdateContainers();
            StatusBar.SetVisible(false);
            ToolBar.SetVisible(false);
            HomeScreen = new HomeScreen(MainGridLayout);
            HomeScreen.SetGridRow(3);
        }

        this.OnTick += _ => Editor.Update();
        this.UI.Update();
        this.Start();

        UI.SizeChanged(new BaseEventArgs());

        UI.RegisterShortcut(new Shortcut(null, new Key(Keycode.G, Keycode.CTRL), e => Editor.Test(), true));

        // If an argument was passed, load that project file and skip the home screen
        if (!string.IsNullOrEmpty(ProjectFile))
        {
            MenuBar.SetVisible(false);
            StatusBar.SetVisible(false);
            ToolBar.SetVisible(false);
            Blue1pxSeparator.SetVisible(false);
            StatusBarDivider.SetVisible(false);
            Graphics.Update(false, true);
            Data.SetProjectPath(ProjectFile);
            CreateEditor();
            Editor.MakeRecentProject();
            MenuBar.SetVisible(true);
            StatusBar.SetVisible(true);
            ToolBar.SetVisible(true);
            Blue1pxSeparator.SetVisible(true);
            StatusBarDivider.SetVisible(true);
        }
    }

    /// <summary>
    /// Initializes the editor after the home screen has been shown.
    /// </summary>
    public void CreateEditor()
    {
        Stopwatch s = new Stopwatch();
        s.Start();

        Editor.LoadProjectSettings();
        ProgressWindow pw = new ProgressWindow("Loading", "Loading project...", true);
        Graphics.Update();
        foreach (float f in Data.LoadGameData())
        {
            // f is percentage of maps that have been parsed
            pw.SetProgress(f);
            // Force redraw in between maps loaded
            if (Graphics.CanUpdate()) Graphics.Update();
            // Window was closed, return to main loop to finish closing program
            else return;
        }

        HomeScreen?.Dispose();
        HomeScreen = null;

        MainGridLayout.Rows[1] = new GridSize(31, Unit.Pixels);
        MainGridLayout.Rows[4] = new GridSize(1, Unit.Pixels);
        MainGridLayout.Rows[5] = new GridSize(26, Unit.Pixels);
        MainGridLayout.UpdateContainers();

        Editor.OptimizeOrder();
        Editor.SetMode(Editor.ProjectSettings.LastMode, true);

        s.Stop();
        StatusBar.QueueMessage($"Project loaded ({s.ElapsedMilliseconds}ms)", true, 5000);
    }

    /// <summary>
    /// Prompts the user to save if there are unsaved changes.
    /// </summary>
    /// <param name="Function">The function to call if saved or continued.</param>
    public void EnsureSaved(Action Function)
    {
        if (!Editor.UnsavedChanges)
        {
            Function();
            return;
        }
        MessageBox box = new MessageBox("Warning", "The game contains unsaved changes. Are you sure you would like to proceed? All unsaved changes will be lost.",
            new List<string>() { "Save", "Continue", "Cancel" }, IconType.Warning);
        box.OnButtonPressed += delegate (BaseEventArgs e)
        {
            if (box.Result == 0) // Save
            {
                Editor.SaveProject();
                Function();
            }
            else if (box.Result == 1) // Continue
            {
                Function();
            }
        };
    }
}
