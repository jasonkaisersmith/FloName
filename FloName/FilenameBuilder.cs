using System;
using System.Collections.Generic;
using System.Text;

namespace FloName
{
    public class FilenameBuilder
    {
        private readonly FilenameGenerator _generator;
        private string _lang;
        private string _format = "{W}-{W}";
        private string? _extension;

        public FilenameBuilder(FilenameGenerator generator, string lang)
        {
            _generator = generator;
            _lang = lang;
        }

        public FilenameBuilder WithFormat(string format)
        {
            _format = format;
            return this;
        }

        public FilenameBuilder WithExtension(string? extension)
        {
            _extension = extension;
            return this;
        }

        public string Generate()
            => _generator.GenerateComplexName(_lang, _format, _extension);

        public IReadOnlyList<string> GenerateBatch(int count)
            => _generator.GenerateBatch(_lang, _format, count, _extension ?? ".txt");
    }
}
