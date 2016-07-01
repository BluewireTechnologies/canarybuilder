using System;

namespace Bluewire.Common.GitWrapper.Parsing.Diff
{
    public class DiffLineFormatter
    {
        public static DiffLineFormatter Default { get; } = new DiffLineFormatter();

        public FormattedDiffNumbering Numbering { get; }

        public DiffLineFormatter(FormattedDiffNumbering numbering = FormattedDiffNumbering.Old)
        {
            this.Numbering = numbering;
        }

        public string Format(GitDiffReader.DiffLine line)
        {
            var lineNumber = Numbering == FormattedDiffNumbering.Old ? line.OldNumber : line.NewNumber;
            var formattedNumber = lineNumber > 0 ? lineNumber.ToString() : "";
            return String.Concat(
                formattedNumber.PadLeft(6),
                ":",
                line.Action.ToString().PadRight(7),
                ":",
                line.Text);
        }
    }
}
