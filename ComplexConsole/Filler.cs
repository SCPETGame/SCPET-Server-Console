using System;
using Terminal.Gui;

namespace ComplexConsole
{
    public class Filler : View
    {
        public int width = 10;
        public int height = 10;

        public Filler(Rect rect) : base(rect)
        {
            width = rect.Width;
            height = rect.Height;
        }

        public Size GetContentSize()
        {
            return new Size(width, height);
        }

        public override void Redraw(Rect bounds)
        {
            Driver.SetAttribute(ColorScheme.Focus);
            Rect f = Frame;
            width = 0;
            height = 0;

            for (int y = 0; y < f.Width; y++)
            {
                Move(0, y);
                int nw = 0;
                for (int x = 0; x < f.Height; x++)
                {
                    Rune rune;
                    switch (x % 3)
                    {
                        case 0:
                            Rune er = y.ToString().ToCharArray(0, 1)[0];
                            nw += er.ToString().Length;
                            Driver.AddRune(er);
                            if (y > 9)
                            {
                                er = y.ToString().ToCharArray(1, 1)[0];
                                nw += er.ToString().Length;
                                Driver.AddRune(er);
                            }
                            rune = '.';
                            break;
                        case 1:
                            rune = 'o';
                            break;
                        default:
                            rune = 'O';
                            break;
                    }
                    Driver.AddRune(rune);
                    nw += Rune.RuneLen(rune);
                }
                if (nw > width)
                {
                    width = nw;
                }
                height = y + 1;
            }
            base.Redraw(bounds);
        }
    }
}