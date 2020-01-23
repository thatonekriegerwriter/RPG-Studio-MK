﻿using System;
using System.Collections.Generic;
using ODL;
using static SDL2.SDL;

namespace MKEditor.Widgets
{
    public class TextArea : Widget
    {
        public string Text { get; protected set; } = "";
        public int TextY { get; protected set; } = 0;
        public int CaretY { get; protected set; } = 2;
        public int CaretHeight { get; protected set; } = 13;
        public Font Font { get; protected set; } = Font.Get("Fonts/ProductSans-M", 14);

        public bool EnteringText = false;

        public int X;
        public int RX;
        public int Width;

        public int CaretIndex = 0;

        public int SelectionStartIndex = -1;
        public int SelectionEndIndex = -1;

        public int SelectionStartX = -1;

        public EventHandler<EventArgs> OnTextChanged;

        public TextArea(object Parent, string Name = "newTextBox")
            : base(Parent, Name)
        {
            Sprites["text"] = new Sprite(this.Viewport);
            Sprites["filler"] = new Sprite(this.Viewport, new SolidBitmap(1, 16, new Color(50, 50, 255, 100)));
            Sprites["filler"].Visible = false;
            Sprites["filler"].Y = 2;
            Sprites["caret"] = new Sprite(this.Viewport, new SolidBitmap(1, 16, Color.WHITE));
            Sprites["caret"].Y = 2;
            OnWidgetSelected += WidgetSelected;
            WidgetIM.OnMouseDown += MouseDown;
            WidgetIM.OnMouseMoving += MouseMoving;
            WidgetIM.OnMouseUp += MouseUp;
            WidgetIM.OnHoverChanged += HoverChanged;
        }

        public void SetInitialText(string Text)
        {
            this.Text = Text;
            X = 0;
            RX = 0;
            CaretIndex = 0;
            DrawText();
            if (OnTextChanged != null) OnTextChanged.Invoke(null, new EventArgs());
        }

        public void SetFont(Font f)
        {
            this.Font = f;
            DrawText();
        }

        public void SetTextY(int TextY)
        {
            this.TextY = TextY;
            Sprites["text"].Y = TextY;
        }

        public void SetCaretY(int CaretY)
        {
            this.CaretY = CaretY;
            Sprites["caret"].Y = Sprites["filler"].Y = CaretY;
        }

        public void SetCaretHeight(int CaretHeight)
        {
            this.CaretHeight = CaretHeight;
            SolidBitmap caret = Sprites["caret"].Bitmap as SolidBitmap;
            caret.SetSize(1, CaretHeight);
            SolidBitmap filler = Sprites["filler"].Bitmap as SolidBitmap;
            filler.SetSize(filler.BitmapWidth, CaretHeight);
        }

        public override void SizeChanged(object sender, SizeEventArgs e)
        {
            base.SizeChanged(sender, e);
            this.Width = Size.Width;
        }

        public override void WidgetSelected(object sender, MouseEventArgs e)
        {
            base.WidgetSelected(sender, e);
            EnteringText = true;
            Input.StartTextInput();
            SetTimer("idle", 400);
        }

        public override void WidgetDeselected(object sender, EventArgs e)
        {
            base.WidgetDeselected(sender, e);
            EnteringText = false;
            Input.StopTextInput();
            if (SelectionStartIndex != -1) CancelSelectionHidden();
        }

        public override void TextInput(object sender, TextInputEventArgs e)
        {
            base.TextInput(sender, e);
            string text = this.Text;
            if (e.Text == "\n")
            {
                Window.UI.SetSelectedWidget(null);
                return;
            }
            else if (!string.IsNullOrEmpty(e.Text))
            {
                if (SelectionStartIndex != -1 && SelectionStartIndex != SelectionEndIndex) DeleteSelection();
                InsertText(CaretIndex, e.Text);
            }
            else if (e.Backspace || e.Delete)
            {
                if (SelectionStartIndex != -1 && SelectionStartIndex != SelectionEndIndex)
                {
                    DeleteSelection();
                }
                else
                {
                    if (SelectionStartIndex == SelectionEndIndex) CancelSelectionHidden();
                    if (e.Delete)
                    {
                        if (CaretIndex < this.Text.Length)
                        {
                            int Count = 1;
                            if (Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL))
                                Count = FindNextCtrlIndex(false) - CaretIndex;
                            MoveCaretRight(Count);
                            RemoveText(this.CaretIndex - Count, Count);
                        }
                    }
                    else
                    {
                        int Count = 1;
                        if (Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL))
                            Count = CaretIndex - FindNextCtrlIndex(true);
                        RemoveText(this.CaretIndex - Count, Count);
                    }
                }
            }
            if (this.Text != text)
            {
                if (this.OnTextChanged != null) this.OnTextChanged.Invoke(null, new EventArgs());
            }
            if (OnTextChanged != null) OnTextChanged.Invoke(null, new EventArgs());
            DrawText();
        }

        /// <summary>
        /// Inserts text to the left of the caret.
        /// </summary>
        /// <param name="InsertionIndex">The index at which to insert text.</param>
        /// <param name="Text">The text to insert.</param>
        public void InsertText(int InsertionIndex, string Text)
        {
            while (Text.Contains("\n")) Text = Text.Replace("\n", "");
            if (Text.Length == 0) return;
            int charw = Font.TextSize(this.Text.Substring(0, InsertionIndex) + Text).Width - Font.TextSize(this.Text.Substring(0, InsertionIndex)).Width;

            if (RX + charw >= Width)
            {
                int left = Width - RX;
                RX += left;
                X += charw - left;
            }
            else
            {
                RX += charw;
            }
            this.Text = this.Text.Insert(InsertionIndex, Text);
            this.CaretIndex += Text.Length;
            ResetIdle();
        }

        /// <summary>
        /// Deletes text to the left of the caret.
        /// </summary>
        /// <param name="StartIndex">Starting index of the range to delete.</param>
        /// <param name="Count">Number of characters to delete.</param>
        public void RemoveText(int StartIndex, int Count = 1)
        {
            if (this.Text.Length == 0 || StartIndex < 0 || StartIndex >= this.Text.Length) return;
            string TextIncluding = this.Text.Substring(0, StartIndex + Count);
            int charw = Font.TextSize(TextIncluding).Width - Font.TextSize(this.Text.Substring(0, StartIndex)).Width;
            int dRX = Math.Min(RX, charw);
            if (RX - charw < 0)
            {
                X -= charw - RX;
                RX = 0;
            }
            else
            {
                RX -= charw;
            }
            int exwidth = Sprites["text"].Bitmap.Width - Width - X - charw;
            if (exwidth < 0)
            {
                RX += Math.Min(X, Math.Abs(exwidth));
                X -= Math.Min(X, Math.Abs(exwidth));
            }
            CaretIndex -= Count;
            this.Text = this.Text.Remove(StartIndex, Count);
            ResetIdle();
        }

        /// <summary>
        /// Deletes the content inside the selection.
        /// </summary>
        public void DeleteSelection()
        {
            int startidx = SelectionStartIndex > SelectionEndIndex ? SelectionEndIndex : SelectionStartIndex;
            int endidx = SelectionStartIndex > SelectionEndIndex ? SelectionStartIndex : SelectionEndIndex;
            CancelSelectionRight();
            RemoveText(startidx, endidx - startidx);
            ResetIdle();
        }

        /// <summary>
        /// Handles input for various keys.
        /// </summary>
        public override void Update()
        {
            base.Update();

            if (!SelectedWidget)
            {
                if (TimerExists("double")) DestroyTimer("double");
                if (TimerExists("left")) DestroyTimer("left");
                if (TimerExists("left_initial")) DestroyTimer("left_initial");
                if (TimerExists("right")) DestroyTimer("right");
                if (TimerExists("right_initial")) DestroyTimer("right_initial");
                if (TimerExists("paste")) DestroyTimer("paste");
                if (EnteringText) WidgetDeselected(null, new EventArgs());
                if (Sprites["caret"].Visible) Sprites["caret"].Visible = false;
                return;
            }

            if (Input.Trigger(SDL_Keycode.SDLK_LEFT) || TimerPassed("left"))
            {
                if (TimerPassed("left")) ResetTimer("left");
                if (CaretIndex > 0)
                {
                    int Count = 1;
                    if (Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL))
                    {
                        Count = CaretIndex - FindNextCtrlIndex(true);
                    }

                    if (Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT))
                    {
                        if (SelectionStartIndex == -1)
                        {
                            SelectionStartX = X + RX;
                            SelectionStartIndex = CaretIndex;
                        }
                        MoveCaretLeft(Count);
                        SelectionEndIndex = CaretIndex;
                        RepositionSprites();
                    }
                    else
                    {
                        if (SelectionStartIndex != -1)
                        {
                            CancelSelectionLeft();
                        }
                        else
                        {
                            MoveCaretLeft(Count);
                            RepositionSprites();
                        }
                    }
                }
                else if (SelectionStartIndex != -1 && !(Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT)))
                {
                    CancelSelectionLeft();
                }
            }
            if (Input.Trigger(SDL_Keycode.SDLK_RIGHT) || TimerPassed("right"))
            {
                if (TimerPassed("right")) ResetTimer("right");
                if (CaretIndex < this.Text.Length)
                {
                    int Count = 1;
                    if (Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL))
                    {
                        Count = FindNextCtrlIndex(false) - CaretIndex;
                    }

                    if (Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT))
                    {
                        if (SelectionStartIndex == -1)
                        {
                            SelectionStartX = X + RX;
                            SelectionStartIndex = CaretIndex;
                        }
                        MoveCaretRight(Count);
                        SelectionEndIndex = CaretIndex;
                        RepositionSprites();
                    }
                    else
                    {
                        if (SelectionStartIndex != -1)
                        {
                            CancelSelectionRight();
                        }
                        else
                        {
                            MoveCaretRight(Count);
                            RepositionSprites();
                        }
                    }
                }
                else if (SelectionStartIndex != -1 && !(Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT)))
                {
                    CancelSelectionRight();
                }
            }
            if (Input.Trigger(SDL_Keycode.SDLK_HOME))
            {
                if (Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT))
                {
                    if (SelectionStartIndex != -1) SelectionEndIndex = 0;
                    else
                    {
                        SelectionStartIndex = CaretIndex;
                        SelectionStartX = X + RX;
                        SelectionEndIndex = 0;
                    }
                }
                else CancelSelectionLeft();
                MoveCaretLeft(CaretIndex);
                RepositionSprites();
            }
            if (Input.Trigger(SDL_Keycode.SDLK_END))
            {
                if (Input.Press(SDL_Keycode.SDLK_LSHIFT) || Input.Press(SDL_Keycode.SDLK_RSHIFT))
                {
                    if (SelectionStartIndex != -1) SelectionEndIndex = this.Text.Length;
                    else
                    {
                        SelectionStartIndex = CaretIndex;
                        SelectionStartX = X + RX;
                        SelectionEndIndex = this.Text.Length;
                    }
                }
                else CancelSelectionRight();
                MoveCaretRight(this.Text.Length - CaretIndex);
                RepositionSprites();
            }
            if (Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL))
            {
                if (Input.Trigger(SDL_Keycode.SDLK_a))
                {
                    SelectAll();
                }
                if (Input.Trigger(SDL_Keycode.SDLK_x))
                {
                    CutSelection();
                }
                if (Input.Trigger(SDL_Keycode.SDLK_c))
                {
                    CopySelection();
                }
                if (Input.Trigger(SDL_Keycode.SDLK_v) || TimerPassed("paste"))
                {
                    PasteText();
                }
            }

            // Timers for repeated input
            if (Input.Press(SDL_Keycode.SDLK_LEFT))
            {
                if (!TimerExists("left_initial") && !TimerExists("left"))
                {
                    SetTimer("left_initial", 300);
                }
                else if (TimerPassed("left_initial"))
                {
                    DestroyTimer("left_initial");
                    SetTimer("left", 50);
                }
            }
            else
            {
                if (TimerExists("left")) DestroyTimer("left");
                if (TimerExists("left_initial")) DestroyTimer("left_initial");
            }
            if (Input.Press(SDL_Keycode.SDLK_RIGHT))
            {
                if (!TimerExists("right_initial") && !TimerExists("right"))
                {
                    SetTimer("right_initial", 300);
                }
                else if (TimerPassed("right_initial"))
                {
                    DestroyTimer("right_initial");
                    SetTimer("right", 50);
                }
            }
            else
            {
                if (TimerExists("right")) DestroyTimer("right");
                if (TimerExists("right_initial")) DestroyTimer("right_initial");
            }
            if ((Input.Press(SDL_Keycode.SDLK_LCTRL) || Input.Press(SDL_Keycode.SDLK_RCTRL)) && Input.Press(SDL_Keycode.SDLK_v))
            {
                if (!TimerExists("paste_initial") && !TimerExists("paste"))
                {
                    SetTimer("paste_initial", 300);
                }
                else if (TimerPassed("paste_initial"))
                {
                    DestroyTimer("paste_initial");
                    SetTimer("paste", 50);
                }
            }
            else
            {
                if (TimerExists("paste")) DestroyTimer("paste");
                if (TimerExists("paste_initial")) DestroyTimer("paste_initial");
            }

            if (TimerPassed("double")) DestroyTimer("double");

            if (TimerPassed("idle"))
            {
                Sprites["caret"].Visible = !Sprites["caret"].Visible;
                ResetTimer("idle");
            }
        }

        /// <summary>
        /// Resets the idle timer, which pauses the caret blinking.
        /// </summary>
        public void ResetIdle()
        {
            Sprites["caret"].Visible = true;
            if (TimerExists("idle")) ResetTimer("idle");
        }

        /// <summary>
        /// Finds the next word that could be skipped to with control.
        /// </summary>
        /// <param name="Left">Whether to search to the left or right of the caret.</param>
        /// <returns>The next index to jump to when holding control.</returns>
        public int FindNextCtrlIndex(bool Left) // or false for Right
        {
            int idx = 0;
            string splitters = " `~!@#$%^&*()-=+[]{}\\|;:'\",.<>/?\n";
            bool found = false;
            if (Left)
            {
                for (int i = CaretIndex - 1; i >= 0; i--)
                {
                    if (splitters.Contains(this.Text[i]) && i != CaretIndex - 1)
                    {
                        idx = i + 1;
                        found = true;
                        break;
                    }
                }
                if (!found) idx = 0;
            }
            else
            {
                for (int i = CaretIndex + 1; i < this.Text.Length; i++)
                {
                    if (splitters.Contains(this.Text[i]))
                    {
                        idx = i;
                        found = true;
                        break;
                    }
                }
                if (!found) idx = this.Text.Length;
            }
            return idx;
        }

        /// <summary>
        /// Cancels the selection and puts the caret on the left.
        /// </summary>
        public void CancelSelectionLeft()
        {
            if (SelectionEndIndex > SelectionStartIndex)
            {
                if (SelectionStartX < X)
                {
                    X = SelectionStartX;
                    RX = 0;
                }
                else
                {
                    RX = SelectionStartX - X;
                }
                CaretIndex -= SelectionEndIndex - SelectionStartIndex;
                RepositionSprites();
            }
            SelectionStartIndex = -1;
            SelectionEndIndex = -1;
            SelectionStartX = -1;
            Sprites["filler"].Visible = false;
        }

        /// <summary>
        /// Cancels the selection and puts the caret on the right.
        /// </summary>
        public void CancelSelectionRight()
        {
            if (SelectionStartIndex > SelectionEndIndex)
            {
                if (SelectionStartX > X + Width)
                {
                    X += SelectionStartX - X - Width;
                    RX = Width;
                }
                else
                {
                    RX = SelectionStartX - X;
                }
                CaretIndex += SelectionStartIndex - SelectionEndIndex;
                RepositionSprites();
            }
            SelectionStartIndex = -1;
            SelectionEndIndex = -1;
            SelectionStartX = -1;
            Sprites["filler"].Visible = false;
        }

        /// <summary>
        /// Cancels the selection without updating the caret.
        /// </summary>
        public void CancelSelectionHidden()
        {
            SelectionStartIndex = -1;
            SelectionEndIndex = -1;
            SelectionStartX = -1;
            Sprites["filler"].Visible = false;
        }

        /// <summary>
        /// Moves the caret to the left.
        /// </summary>
        /// <param name="Count">The number of characters to skip.</param>
        public void MoveCaretLeft(int Count = 1)
        {
            if (CaretIndex - Count < 0) return;
            string TextToCaret = this.Text.Substring(0, CaretIndex);
            int charw = Font.TextSize(TextToCaret).Width - Font.TextSize(TextToCaret.Substring(0, TextToCaret.Length - Count)).Width;
            if (RX - charw < 0)
            {
                X -= charw - RX;
                RX = 0;
            }
            else
            {
                RX -= charw;
            }
            CaretIndex -= Count;
            ResetIdle();
        }

        /// <summary>
        /// Moves the caret to the right.
        /// </summary>
        /// <param name="Count">The number of characters to skip.</param>
        public void MoveCaretRight(int Count = 1)
        {
            if (CaretIndex + Count > this.Text.Length) return;
            string TextToCaret = this.Text.Substring(0, CaretIndex);
            string TextToCaretPlusOne = this.Text.Substring(0, CaretIndex + Count);
            int charw = Font.TextSize(TextToCaretPlusOne).Width - Font.TextSize(TextToCaret).Width;
            if (RX + charw >= Width)
            {
                int left = Width - RX;
                RX += left;
                X += charw - left;
            }
            else
            {
                RX += charw;
            }
            CaretIndex += Count;
            ResetIdle();
        }

        /// <summary>
        /// Determines key values based on the given mouse position.
        /// </summary>
        /// <returns>List<int>() { RX, X, found }</int></returns>
        public List<int> GetMousePosition(MouseEventArgs e)
        {
            int RetRX = RX;
            int RetCaretIndex = CaretIndex;
            int Found = 0;
            int rmx = e.X - Viewport.X;
            if (rmx < 0 || rmx >= Width) return null;
            for (int i = 0; i < this.Text.Length; i++)
            {
                int fullwidth = Font.TextSize(this.Text.Substring(0, i)).Width;
                int charw = Font.TextSize(this.Text.Substring(0, i + 1)).Width - Font.TextSize(this.Text.Substring(0, i)).Width;
                int rx = fullwidth - X;
                if (rx >= 0 && rx < Width)
                {
                    if (rmx >= rx && rmx < rx + charw)
                    {
                        int diff = rx + charw - rmx;
                        if (diff > charw / 2)
                        {
                            RetRX = rx;
                            RetCaretIndex = i;
                        }
                        else
                        {
                            RetRX = rx + charw;
                            RetCaretIndex = i + 1;
                        }
                        Found = 1;
                        break;
                    }
                }
            }
            return new List<int>() { RetRX, RetCaretIndex, Found };
        }

        /// <summary>
        /// Redraws the text bitmap.
        /// </summary>
        public void DrawText()
        {
            RepositionSprites();
            if (Sprites["text"].Bitmap != null) Sprites["text"].Bitmap.Dispose();
            if (string.IsNullOrEmpty(this.Text)) return;
            Size s = Font.TextSize(this.Text);
            if (s.Width < 1 || s.Height < 1) return;
            Sprites["text"].Bitmap = new Bitmap(s);
            Sprites["text"].Bitmap.Unlock();
            Sprites["text"].Bitmap.Font = this.Font;
            Sprites["text"].Bitmap.DrawText(this.Text, Color.WHITE);
            Sprites["text"].Bitmap.Lock();
        }

        /// <summary>
        /// Repositions the text, caret and selection sprites.
        /// </summary>
        public void RepositionSprites()
        {
            Sprites["text"].X = -X;
            int add = 1;
            if (this.Text.Length > 0 && CaretIndex > 0 && this.Text[CaretIndex - 1] == ' ' && RX != Width) add = 0;
            Sprites["caret"].X = Math.Max(0, RX - add);

            // Selections
            if (SelectionStartIndex > SelectionEndIndex)
            {
                if (X + Width < SelectionStartX)
                {
                    Sprites["filler"].X = RX;
                    (Sprites["filler"].Bitmap as SolidBitmap).SetSize(Width - RX, CaretHeight);
                    Sprites["filler"].Visible = true;
                }
                else
                {
                    Sprites["filler"].X = RX;
                    (Sprites["filler"].Bitmap as SolidBitmap).SetSize(SelectionStartX - X - RX, CaretHeight);
                    Sprites["filler"].Visible = true;
                }
            }
            else if (SelectionStartIndex < SelectionEndIndex)
            {
                if (SelectionStartX < X)
                {
                    Sprites["filler"].X = 0;
                    (Sprites["filler"].Bitmap as SolidBitmap).SetSize(RX, CaretHeight);
                    Sprites["filler"].Visible = true;
                }
                else
                {
                    Sprites["filler"].X = SelectionStartX - X;
                    (Sprites["filler"].Bitmap as SolidBitmap).SetSize(X + RX - SelectionStartX, CaretHeight);
                    Sprites["filler"].Visible = true;
                }
            }
            else
            {
                if (SelectionStartIndex != -1)
                {
                    Sprites["filler"].Visible = false;
                }
            }
        }

        /// <summary>
        /// Selects all text.
        /// </summary>
        public void SelectAll()
        {
            MoveCaretRight(this.Text.Length - CaretIndex);
            SelectionStartIndex = 0;
            SelectionEndIndex = this.Text.Length;
            SelectionStartX = 0;
            RepositionSprites();
        }

        /// <summary>
        /// Copies the selected text to the clipboard and deletes the selection.
        /// </summary>
        public void CutSelection()
        {
            if (SelectionStartIndex != -1)
            {
                int startidx = SelectionStartIndex > SelectionEndIndex ? SelectionEndIndex : SelectionStartIndex;
                int endidx = SelectionStartIndex > SelectionEndIndex ? SelectionStartIndex : SelectionEndIndex;
                string text = this.Text.Substring(startidx, endidx - startidx);
                SDL_SetClipboardText(text);
                DeleteSelection();
                DrawText();
            }
        }

        /// <summary>
        /// Copies the selected text to the clipboard.
        /// </summary>
        public void CopySelection()
        {
            if (SelectionStartIndex != -1)
            {
                int startidx = SelectionStartIndex > SelectionEndIndex ? SelectionEndIndex : SelectionStartIndex;
                int endidx = SelectionStartIndex > SelectionEndIndex ? SelectionStartIndex : SelectionEndIndex;
                string text = this.Text.Substring(startidx, endidx - startidx);
                SDL_SetClipboardText(text);
            }
        }

        /// <summary>
        /// Pastes text from the clipboard to the text field.
        /// </summary>
        public void PasteText()
        {
            if (TimerPassed("paste")) ResetTimer("paste");
            string text = SDL_GetClipboardText();
            if (SelectionStartIndex != -1 && SelectionStartIndex != SelectionEndIndex) DeleteSelection();
            InsertText(CaretIndex, text);
            DrawText();
        }

        public override void MouseDown(object sender, MouseEventArgs e)
        {
            base.MouseDown(sender, e);
            if (!WidgetIM.Hovering || this.Text.Length == 0) return;
            if (SelectionStartIndex != -1 && SelectionStartIndex != SelectionEndIndex) CancelSelectionHidden();
            int OldRX = RX;
            int OldCaretIndex = CaretIndex;
            List<int> newvals = GetMousePosition(e);
            RX = newvals[0];
            CaretIndex = newvals[1];
            bool found = newvals[2] == 1;
            if (!found)
            {
                if (Sprites["text"].Bitmap.Width - X < Width) // No extra space to the right that could be scrolled to
                {
                    RX = Sprites["text"].Bitmap.Width;
                    CaretIndex = this.Text.Length;
                }
            }
            RepositionSprites();
            if (!TimerExists("double"))
            {
                SetTimer("double", 300);
            }
            else if (!TimerPassed("double"))
            {
                // Double clicked
                DoubleClick();
                DestroyTimer("double");
            }
        }

        public void DoubleClick()
        {
            int startindex = FindNextCtrlIndex(true);
            int endindex = FindNextCtrlIndex(false);
            if (endindex - startindex > 0)
            {
                SelectionStartIndex = startindex;
                SelectionEndIndex = endindex;
                MoveCaretLeft(CaretIndex - startindex);
                SelectionStartX = RX + X;
                MoveCaretRight(endindex - CaretIndex);
                RepositionSprites();
            }
        }

        public override void MouseMoving(object sender, MouseEventArgs e)
        {
            base.MouseMoving(sender, e);
            if (!e.LeftButton || WidgetIM.ClickedLeftInArea != true) return;
            int OldRX = RX;
            int OldCaretIndex = CaretIndex;
            int rmx = e.X - Viewport.X;
            if (rmx >= Width && Sprites["text"].Bitmap.Width - X >= Width)
            {
                MoveCaretRight();
                SelectionEndIndex = CaretIndex;
                RepositionSprites();
                return;
            }
            else if (rmx < 0 && X > 0)
            {
                MoveCaretLeft();
                SelectionEndIndex = CaretIndex;
                RepositionSprites();
                return;
            }
            List<int> newvals = GetMousePosition(e);
            if (newvals == null)
            {
                if (rmx < 0)
                {
                    MoveCaretLeft();
                    SelectionEndIndex = CaretIndex;
                    RepositionSprites();
                }
                return;
            }
            RX = newvals[0];
            CaretIndex = newvals[1];
            bool found = newvals[2] == 1;
            if (found && CaretIndex != OldCaretIndex)
            {
                if (SelectionStartIndex == -1)
                {
                    SelectionStartIndex = OldCaretIndex;
                    SelectionStartX = OldRX + X;
                }
                SelectionEndIndex = CaretIndex;
                RepositionSprites();
            }
        }

        public override void MouseUp(object sender, MouseEventArgs e)
        {
            base.MouseUp(sender, e);
        }

        public override void HoverChanged(object sender, MouseEventArgs e)
        {
            base.HoverChanged(sender, e);
            if (WidgetIM.Hovering)
            {
                Input.SetCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM);
            }
            else
            {
                Input.SetCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW);
            }
        }
    }
}
