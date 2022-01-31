using System.Text;

namespace NVerilogFormatter
{
    public class RawFormatterLine
    {
        public int IdentLevel { get; set; } = 0;

        public string Text { get; set; } = "";

        public override string ToString()
        {
            var b = new StringBuilder();
            for (var i = 0; i < IdentLevel; i++)
            {
                b.Append(" ");
            }

            b.Append(Text);

            return b.ToString();
        }
    }
}
