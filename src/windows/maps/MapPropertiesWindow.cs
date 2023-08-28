﻿using System;
using System.Collections.Generic;
using RPGStudioMK.Game;

namespace RPGStudioMK.Widgets;

public class MapPropertiesWindow : PopupWindow
{
    public Map Map;
    public Map OldMap;

    public bool UnsavedChanges = false;
    public bool UpdateMapViewer = false;

    bool MakeUndoEvent;

    TextBox MapName;
    NumericBox Width;
    NumericBox Height;
    DropdownBox BGM;
    DropdownBox BGS;
    ListBox Tilesets;
    Button EditTilesetsButton;
    //ListBox Autotiles;

    public MapPropertiesWindow(Map OldMap, bool MakeUndoEvent = true)
    {
        this.OldMap = OldMap;
        this.Map = (Map) OldMap.Clone();
        this.MakeUndoEvent = MakeUndoEvent;
        this.SetTitle($"Map Properties");
        MinimumSize = MaximumSize = new Size(387, 312);
        SetSize(MaximumSize);
        this.Center();

        Label settings = new Label(this);
        settings.SetText("Info");
        settings.SetFont(Fonts.ParagraphBold);
        settings.SetPosition(19, 33);

        GroupBox box1 = new GroupBox(this);
        box1.SetPosition(19, 54);
        box1.SetSize(350, 213);

        Label namelabel = new Label(box1);
        namelabel.SetText("Map Name:");
        namelabel.SetFont(Fonts.Paragraph);
        namelabel.SetPosition(7, 6);
        MapName = new TextBox(box1);
        MapName.SetPosition(6, 22);
        MapName.SetSize(186, 27);
        MapName.SetText(this.Map.Name);
        MapName.OnTextChanged += _ =>
        {
            this.Map.Name = MapName.Text;
        };

        Label widthlabel = new Label(box1);
        widthlabel.SetText("Width:");
        widthlabel.SetFont(Fonts.Paragraph);
        widthlabel.SetPosition(7, 54);
        Width = new NumericBox(box1);
        Width.SetPosition(6, 70);
        Width.SetMinValue(1);
        Width.SetMaxValue(255);
        Width.SetSize(90, 27);
        Width.SetValue(this.Map.Width);
        Width.OnValueChanged += delegate (BaseEventArgs e)
        {
            this.Map.Width = Width.Value;
        };

        Label heightlabel = new Label(box1);
        heightlabel.SetText("Height:");
        heightlabel.SetFont(Fonts.Paragraph);
        heightlabel.SetPosition(108, 54);
        Height = new NumericBox(box1);
        Height.SetPosition(107, 70);
        Height.SetMinValue(1);
        Height.SetMaxValue(255);
        Height.SetSize(90, 27);
        Height.SetValue(this.Map.Height);
        Height.OnValueChanged += delegate (BaseEventArgs e)
        {
            this.Map.Height = Height.Value;
        };

        CheckBox autoplaybgm = new CheckBox(box1);
        autoplaybgm.SetPosition(7, 106);
        autoplaybgm.SetText("Autoplay BGM");
        autoplaybgm.SetFont(Fonts.Paragraph);
        autoplaybgm.SetChecked(this.Map.AutoplayBGM);
        autoplaybgm.OnCheckChanged += delegate (BaseEventArgs e)
        {
            this.Map.AutoplayBGM = autoplaybgm.Checked;
            BGM.SetEnabled(autoplaybgm.Checked);
            BGM.SetText(autoplaybgm.Checked ? Map.BGM.Name : "");
        };
        BGM = new DropdownBox(box1);
        BGM.SetPosition(6, 123);
        BGM.SetSize(186, 27);
        BGM.SetText(this.Map.AutoplayBGM ? this.Map.BGM.Name : "");
        BGM.SetEnabled(this.Map.AutoplayBGM);
        BGM.SetReadOnly(true);
        BGM.OnDropDownClicked += delegate (BaseEventArgs e)
        {
            AudioPicker picker = new AudioPicker("Audio/BGM", this.Map.BGM.Name, this.Map.BGM.Volume, this.Map.BGM.Pitch);
            picker.OnClosed += delegate (BaseEventArgs _)
            {
                if (picker.Result != null)
                {
                    this.Map.BGM.Name = picker.Result.Value.Filename;
                    this.Map.BGM.Volume = picker.Result.Value.Volume;
                    this.Map.BGM.Pitch = picker.Result.Value.Pitch;
                    BGM.SetText(this.Map.BGM.Name);
                }
            };
        };

        CheckBox autoplaybgs = new CheckBox(box1);
        autoplaybgs.SetPosition(7, 161);
        autoplaybgs.SetText("Autoplay BGS");
        autoplaybgs.SetFont(Fonts.Paragraph);
        autoplaybgs.SetChecked(this.Map.AutoplayBGS);
        autoplaybgs.OnCheckChanged += delegate (BaseEventArgs e)
        {
            this.Map.AutoplayBGS = autoplaybgs.Checked;
            BGS.SetEnabled(autoplaybgs.Checked);
            BGS.SetText(autoplaybgs.Checked ? this.Map.BGS.Name : "");
        };
        BGS = new DropdownBox(box1);
        BGS.SetPosition(6, 178);
        BGS.SetSize(186, 27);
        BGS.SetText(this.Map.AutoplayBGS ? this.Map.BGS.Name : "");
        BGS.SetEnabled(this.Map.AutoplayBGS);
        BGS.SetReadOnly(true);
        BGS.OnDropDownClicked += delegate (BaseEventArgs e)
        {
            AudioPicker picker = new AudioPicker("Audio/BGS", this.Map.BGS.Name, this.Map.BGS.Volume, this.Map.BGS.Pitch);
            picker.OnClosed += delegate (BaseEventArgs _)
            {
                if (picker.Result != null)
                {
                    this.Map.BGS.Name = picker.Result.Value.Filename;
                    this.Map.BGS.Volume = picker.Result.Value.Volume;
                    this.Map.BGS.Pitch = picker.Result.Value.Pitch;
                    BGS.SetText(this.Map.BGS.Name);
                }
            };
        };

        Tilesets = new ListBox(box1);
        Tilesets.SetPosition(212, 22);
        List<ListItem> tilesetitems = new List<ListItem>();
        for (int i = 0; i < this.Map.TilesetIDs.Count; i++)
        {
            int id = this.Map.TilesetIDs[i];
            Tileset tileset = Data.Tilesets[id];
            tilesetitems.Add(new ListItem(tileset));
        }
        Tilesets.SetSize(132, 150);
        Tilesets.SetItems(tilesetitems);

        EditTilesetsButton = new Button(box1);
        EditTilesetsButton.SetPosition(206, 176);
        EditTilesetsButton.SetSize(144, 32);
        EditTilesetsButton.SetText("Edit Tileset");
        EditTilesetsButton.OnClicked += _ => AddTileset();

        /*Autotiles = new ListBox(box1);
        Autotiles.SetPosition(312, 22);
        List<ListItem> autotileitems = new List<ListItem>();
        for (int i = 0; i < this.Map.AutotileIDs.Count; i++)
        {
            int id = this.Map.AutotileIDs[i];
            Autotile autotile = Data.Autotiles[id];
            autotileitems.Add(new ListItem(autotile));
        }
        Autotiles.SetItems(autotileitems);
        Autotiles.SetButtonText("Add Autotile");
        Autotiles.ListDrawer.OnButtonClicked += AddAutotile;*/

        Label tilesetslabel = new Label(box1);
        tilesetslabel.SetText("Tileset:");
        tilesetslabel.SetFont(Fonts.Paragraph);
        tilesetslabel.SetPosition(213, 6);

        //Label autotileslabel = new Label(box1);
        //autotileslabel.SetText("Autotiles:");
        //autotileslabel.SetFont(f);
        //autotileslabel.SetPosition(313, 6);

        CreateButton("Cancel", _ => Cancel());
        CreateButton("OK", _ => OK());

        RegisterShortcuts(new List<Shortcut>()
        {
            new Shortcut(this, new Key(Keycode.ENTER, Keycode.CTRL), _ => OK(), true)
        });
    }

    public void AddTileset()
    {
        TilesetPicker picker = new TilesetPicker(Map);
        picker.OnClosed += delegate (BaseEventArgs e2)
        {
            bool update = false;
            if (Map.TilesetIDs.Count != picker.ResultIDs.Count) update = true;
            if (!update)
            {
                for (int i = 0; i < picker.ResultIDs.Count; i++)
                {
                    if (picker.ResultIDs[i] != Map.TilesetIDs[i])
                    {
                        update = true;
                        break;
                    }
                }
            }
            if (update)
            {
                Map.TilesetIDs = picker.ResultIDs;
                List<ListItem> tilesetitems = new List<ListItem>();
                for (int i = 0; i < this.Map.TilesetIDs.Count; i++)
                {
                    int id = this.Map.TilesetIDs[i];
                    Tileset tileset = Data.Tilesets[id];
                    tilesetitems.Add(new ListItem(tileset));
                }
                Tilesets.SetItems(tilesetitems);
                Map.AutotileIDs.Clear();
                for (int i = 0; i < 7; i++)
                {
                    Map.AutotileIDs.Add(Data.Tilesets[Map.TilesetIDs[0]].ID * 7 + i);
                }
            }
        };
    }

    /*public void AddAutotile(BaseEventArgs e)
    {
        AutotilePicker picker = new AutotilePicker(Map);
        picker.OnClosed += delegate (BaseEventArgs e2)
        {
            bool update = false;
            if (Map.AutotileIDs.Count != picker.ResultIDs.Count) update = true;
            if (!update)
            {
                for (int i = 0; i < picker.ResultIDs.Count; i++)
                {
                    if (picker.ResultIDs[i] != Map.AutotileIDs[i])
                    {
                        update = true;
                        break;
                    }
                }
            }
            if (update)
            {
                Map.AutotileIDs = picker.ResultIDs;
                List<ListItem> autotileitems = new List<ListItem>();
                for (int i = 0; i < this.Map.AutotileIDs.Count; i++)
                {
                    int id = this.Map.AutotileIDs[i];
                    Autotile autotile = Data.Autotiles[id];
                    autotileitems.Add(new ListItem(autotile));
                }
                Autotiles.SetItems(autotileitems);
            }
        };
    }*/

    public void OK()
    {
        this.UpdateMapViewer = true;
        Action Finalize = delegate
        {
            Close();
        };
        Action Continue = delegate
        {
            #region Autotile conversion for multiple tilesets per map
            // Updates autotiles
            /*bool autotileschanged = false;
            if (Map.AutotileIDs.Count != OldMap.AutotileIDs.Count) autotileschanged = true;
            if (!autotileschanged)
            {
                for (int i = 0; i < Map.AutotileIDs.Count; i++)
                {
                    if (Map.AutotileIDs[i] != OldMap.AutotileIDs[i])
                    {
                        autotileschanged = true;
                        break;
                    }
                }
            }
            if (autotileschanged)
            {
                UnsavedChanges = true;
                bool warn = false;
                for (int layer = 0; layer < Map.Layers.Count; layer++)
                {
                    for (int i = 0; i < Map.Width * Map.Height; i++)
                    {
                        if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Tileset) continue;
                        int autotileID = OldMap.AutotileIDs[Map.Layers[layer].Tiles[i].Index];
                        if (!Map.AutotileIDs.Contains(autotileID))
                        {
                            warn = true;
                            break;
                        }
                    }
                }
                if (warn)
                {
                    MessageBox msg = new MessageBox("Warning", "One of the deleted autotiles was still in use. By choosing to continue, tiles of that autotile will be deleted.", new List<string>() { "Continue", "Cancel" }, IconType.Warning);
                    msg.OnButtonPressed += delegate (BaseEventArgs e2)
                    {
                        if (msg.Result == 0) // Continue
                        {
                            for (int layer = 0; layer < Map.Layers.Count; layer++)
                            {
                                for (int i = 0; i < Map.Width * Map.Height; i++)
                                {
                                    if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Tileset) continue;
                                    int autotileID = OldMap.AutotileIDs[Map.Layers[layer].Tiles[i].Index];
                                    if (!Map.AutotileIDs.Contains(autotileID))
                                    {
                                        Map.Layers[layer].Tiles[i] = null;
                                    }
                                    else Map.Layers[layer].Tiles[i].Index = Map.AutotileIDs.IndexOf(autotileID);
                                }
                            }
                            Finalize();
                        }
                        else if (msg.Result == 1) // Cancel
                        {
                            UnsavedChanges = false;
                            UpdateMapViewer = false;
                        }
                    };
                }
                else
                {
                    for (int layer = 0; layer < Map.Layers.Count; layer++)
                    {
                        for (int i = 0; i < Map.Width * Map.Height; i++)
                        {
                            if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Tileset) continue;
                            int autotileID = OldMap.AutotileIDs[Map.Layers[layer].Tiles[i].Index];
                            if (!Map.AutotileIDs.Contains(autotileID))
                            {
                                throw new Exception("Unknown");
                            }
                            else Map.Layers[layer].Tiles[i].Index = Map.AutotileIDs.IndexOf(autotileID);
                        }
                    }
                    Finalize();
                }
            }
            if (!autotileschanged) Finalize();*/
            #endregion
            Finalize();
        };
        // Resizes Map
        if (Map.Width != OldMap.Width || Map.Height != OldMap.Height)
        {
            List<Layer> OldLayers = OldMap.Layers.ConvertAll(l => (Layer) l.Clone());
            Size OldSize = new Size(OldMap.Width, OldMap.Height);
            Map.Resize(OldMap.Width, Map.Width, OldMap.Height, Map.Height);
            UnsavedChanges = true;
        }
        // Marks name change
        if (Map.Name != OldMap.Name)
        {
            UnsavedChanges = true;
        }
        // Marks BGM changes
        if (!Map.BGM.Equals(OldMap.BGM) || Map.AutoplayBGM != OldMap.AutoplayBGM)
        {
            UnsavedChanges = true;
        }
        // Marks BGS changes
        if (!Map.BGS.Equals(OldMap.BGS) || Map.AutoplayBGS != OldMap.AutoplayBGS)
        {
            UnsavedChanges = true;
        }
        // Updates tilesets
        bool tilesetschanged = false;
        if (Map.TilesetIDs.Count != OldMap.TilesetIDs.Count) tilesetschanged = true;
        if (!tilesetschanged)
        {
            for (int i = 0; i < Map.TilesetIDs.Count; i++)
            {
                if (Map.TilesetIDs[i] != OldMap.TilesetIDs[i])
                {
                    tilesetschanged = true;
                    break;
                }
            }
        }
        if (tilesetschanged)
        {
            UnsavedChanges = true;
            #region Tileset conversion for multiple tilesets per map
            /*bool warn = false;
            for (int layer = 0; layer < Map.Layers.Count; layer++)
            {
                for (int i = 0; i < Map.Width * Map.Height; i++)
                {
                    if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Autotile) continue;
                    int tilesetID = OldMap.TilesetIDs[Map.Layers[layer].Tiles[i].Index];
                    if (!Map.TilesetIDs.Contains(tilesetID))
                    {
                        warn = true;
                        break;
                    }
                }
            }
            if (warn)
            {
                MessageBox msg = new MessageBox("Warning", "One of the deleted tilesets was still in use. By choosing to continue, tiles of that tileset will be deleted.", new List<string>() { "Continue", "Cancel" }, IconType.Warning);
                msg.OnButtonPressed += delegate (BaseEventArgs e2)
                {
                    if (msg.Result == 0) // Continue
                    {
                        for (int layer = 0; layer < Map.Layers.Count; layer++)
                        {
                            for (int i = 0; i < Map.Width * Map.Height; i++)
                            {
                                if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Autotile) continue;
                                int tilesetID = OldMap.TilesetIDs[Map.Layers[layer].Tiles[i].Index];
                                if (!Map.TilesetIDs.Contains(tilesetID))
                                {
                                    Map.Layers[layer].Tiles[i] = null;
                                }
                                else Map.Layers[layer].Tiles[i].Index = Map.TilesetIDs.IndexOf(tilesetID);
                            }
                        }
                        Continue();
                    }
                    else if (msg.Result == 1) // Cancel
                    {
                        UnsavedChanges = false;
                        UpdateMapViewer = false;
                    }
                };
            }
            else
            {
                for (int layer = 0; layer < Map.Layers.Count; layer++)
                {
                    for (int i = 0; i < Map.Width * Map.Height; i++)
                    {
                        if (Map.Layers[layer].Tiles[i] == null || Map.Layers[layer].Tiles[i].TileType == TileType.Autotile) continue;
                        int tilesetID = OldMap.TilesetIDs[Map.Layers[layer].Tiles[i].Index];
                        if (!Map.TilesetIDs.Contains(tilesetID))
                        {
                            throw new Exception("Impossible-to-reach code has been reached.");
                        }
                        else Map.Layers[layer].Tiles[i].Index = Map.TilesetIDs.IndexOf(tilesetID);
                    }
                }
                Continue();
            }*/
            #endregion
            Continue();
        }
        if (!tilesetschanged) Continue();
    }

    public void Cancel()
    {
        Close();
    }
}
