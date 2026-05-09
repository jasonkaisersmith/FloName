using FloName;
using FloName.Providers;
using NUnit.Framework;

namespace Test_FloName
{
    [TestFixture]
    public class NameGeneratorTests
    {
        #region Setup
        private string[] _dictionary;
        private NameContext _context;
        private NameGenerator _generator;

        private static TokenProviderRegistry BuildRegistry()
        {
            var registry = new TokenProviderRegistry(
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            registry.Register(new WordTokenProvider());
            registry.Register(new AlphaTokenProvider());
            registry.Register(new NumberTokenProvider());
            registry.Register(new AlphaNumericTokenProvider());
            registry.Register(new DateTokenProvider());
            registry.Register(new SeqTokenProvider());
            return registry;
        }

        private static NameGenerator BuildGenerator(string[] dictionary, NameContext context)
            => new(dictionary, context, BuildRegistry(), Random.Shared);

        [SetUp]
        public void SetUp()
        {
            _dictionary = ["apple", "River", "table", "Forest", "stone", "Cloud"];
            _context = new NameContext();
            _generator = BuildGenerator(_dictionary, _context);
        }
        #endregion

        #region Literal passthrough 
        [Test]
        public void Generate_LiteralsOnly_ReturnedAsIs()
            => Assert.That(_generator.GenerateComplexName("hello-world"), Is.EqualTo("hello-world"));

        [Test]
        public void Generate_EmptyFormat_ReturnsEmptyString()
            => Assert.That(_generator.GenerateComplexName(""), Is.EqualTo(""));

        // ── {a} / {A} ──────────────────────────────────────────────────────

        [Test]
        public void Generate_LowercaseLetter_ReturnsSingleLowercaseLetter()
        {
            var result = _generator.GenerateComplexName("{a}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Length.EqualTo(1));
                Assert.That(char.IsLower(result[0]));
            }
        }

        [Test]
        public void Generate_UppercaseLetter_ReturnsSingleUppercaseLetter()
        {
            var result = _generator.GenerateComplexName("{A}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Length.EqualTo(1));
                Assert.That(char.IsUpper(result[0]));
            }
        }
        #endregion

        #region ── {n} / {N} ──────────────────────────────────────────────────────

        [Test]
        public void Generate_Digit_ReturnsSingleDigit()
        {
            var result = _generator.GenerateComplexName("{n}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Length.EqualTo(1));
                Assert.That(char.IsDigit(result[0]));
            }
        }

        [Test]
        public void Generate_NonZeroDigit_ReturnsDigitBetween1And9()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = _generator.GenerateComplexName("{N}");
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result, Has.Length.EqualTo(1));
                    Assert.That(int.Parse(result), Is.InRange(1, 9));
                }
            }
        }
        #endregion

        #region ── {an} / {An} / {AN} ─────────────────────────────────────────────

        [Test]
        public void Generate_AlphaNumericLower_ReturnsLowercaseLetterOrDigit()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = _generator.GenerateComplexName("{an}");
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result, Has.Length.EqualTo(1));
                    Assert.That(char.IsLetterOrDigit(result[0]));
                    Assert.That(char.IsUpper(result[0]), Is.False);
                }
            }
        }

        [Test]
        public void Generate_AlphaNumericUpper_ReturnsUppercaseLetterOrDigit()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = _generator.GenerateComplexName("{AN}");
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result, Has.Length.EqualTo(1));
                    Assert.That(char.IsLetterOrDigit(result[0]));
                    Assert.That(char.IsLower(result[0]), Is.False);
                }
            }
        }

        [Test]
        public void Generate_AlphaNumericUpperNoZero_NeverReturnsZero()
        {
            for (int i = 0; i < 200; i++)
            {
                var result = _generator.GenerateComplexName("{AN}");
                Assert.That(result, Is.Not.EqualTo("0"));
            }
        }
        #endregion

        #region ── {w} / {W} ──────────────────────────────────────────────────────

        [Test]
        public void Generate_LowercaseWord_ReturnsWordFromDictionaryLowercased()
        {
            var result = _generator.GenerateComplexName("{w}");
            var lowercased = _dictionary.Select(w => w.ToLower()).ToArray();
            Assert.That(lowercased, Contains.Item(result));
        }

        [Test]
        public void Generate_UppercaseWord_ReturnsWordFromDictionaryOriginalCasing()
        {
            var result = _generator.GenerateComplexName("{W}");
            Assert.That(_dictionary, Contains.Item(result));
        }
        #endregion

        #region ── Repeat modifier ────────────────────────────────────────────────

        [Test]
        public void Generate_RepeatLetter_ReturnsCorrectLength()
        {
            var result = _generator.GenerateComplexName("{A:4}");
            Assert.That(result, Has.Length.EqualTo(4));
            Assert.That(result.All(char.IsUpper));
        }

        [Test]
        public void Generate_RepeatDigit_ReturnsCorrectLength()
        {
            var result = _generator.GenerateComplexName("{n:3}");
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result.All(char.IsDigit));
        }

        [Test]
        public void Generate_RepeatOne_BehavesLikeNoRepeat()
        {
            var result = _generator.GenerateComplexName("{A:1}");
            Assert.That(result, Has.Length.EqualTo(1));
        }

        [Test]
        public void Generate_InvalidRepeatCount_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{A:0}"));

        [Test]
        public void Generate_NonNumericRepeatCount_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{A:abc}"));
        #endregion

        #region ── Unique modifier ────────────────────────────────────────────────

        [Test]
        public void Generate_UniqueTokens_AllDifferent()
        {
            var generator = BuildGenerator(_dictionary, new NameContext());
            var format = string.Concat(Enumerable.Repeat("{AU}", 20));
            var result = generator.GenerateComplexName(format);

            Assert.That(result, Has.Length.EqualTo(20));
            Assert.That(result.Distinct().Count(), Is.EqualTo(20));
        }

        [Test]
        public void Generate_UniqueExhausted_ThrowsInvalidOperationException()
        {
            var generator = BuildGenerator(_dictionary, new NameContext());
            var format = string.Concat(Enumerable.Repeat("{AU}", 27));
            Assert.Throws<InvalidOperationException>(
                () => generator.GenerateComplexName(format));
        }

        [Test]
        public void Generate_UniqueLowercaseU_TreatedAsUnique()
        {
            var generator = BuildGenerator(_dictionary, new NameContext());
            var format = string.Concat(Enumerable.Repeat("{Au}", 20));
            var result = generator.GenerateComplexName(format);

            Assert.That(result, Has.Length.EqualTo(20));
            Assert.That(result.Distinct().Count(), Is.EqualTo(20));
        }
        #endregion

        #region ── DATE token ─────────────────────────────────────────────────────

        [Test]
        public void Generate_DateToken_ReturnsFormattedDate()
        {
            var result = _generator.GenerateComplexName("{DATE:yyyy-MM-dd}");
            Assert.That(DateTime.TryParseExact(result, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _));
        }

        [Test]
        public void Generate_DateTokenLowercase_ReturnsFormattedDate()
        {
            var result = _generator.GenerateComplexName("{date:yyyy-MM-dd}");
            Assert.That(DateTime.TryParseExact(result, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _));
        }

        [Test]
        public void Generate_DateTokenMissingFormat_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{DATE:}"));

        [Test]
        public void Generate_DateTokenMissingColon_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{DATE}"));
        #endregion

        #region ── Composite formats ──────────────────────────────────────────────

        [Test]
        public void Generate_CompositeFormat_CorrectStructure()
        {
            var result = _generator.GenerateComplexName("report_{DATE:yyyyMMdd}-{A:4}");
            Assert.That(result, Does.Match(@"^report_\d{8}-[A-Z]{4}$"));
        }

        [Test]
        public void Generate_LiteralsAndTokensMixed_CorrectOutput()
        {
            var result = _generator.GenerateComplexName("prefix-{A}-{n}-suffix");
            Assert.That(result, Does.Match(@"^prefix-[A-Z]-\d-suffix$"));
        }
        #endregion

        #region ── Error handling ─────────────────────────────────────────────────

        [Test]
        public void Generate_UnknownToken_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{Z}"));

        [Test]
        public void Generate_UnterminatedToken_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{A"));
        #endregion

        #region ── Help ───────────────────────────────────────────────────────────

        [Test]
        public void Help_ReturnsNonEmptyString()
            => Assert.That(FilenameGenerator.Help(), Is.Not.Null.And.Not.Empty);

        [Test]
        public void Help_ContainsKeyTokens()
        {
            var help = FilenameGenerator.Help();
            Assert.Multiple(() =>
            {
                Assert.That(help, Does.Contain("{A}"));
                Assert.That(help, Does.Contain("{w}"));
                Assert.That(help, Does.Contain("{DATE:format}"));
                Assert.That(help, Does.Contain(":N"));
                Assert.That(help, Does.Contain("U"));
            });
        }
        #endregion

        #region ── Repeat with separator ─────────────────────────────────────────

        [Test]
        public void Generate_RepeatWordWithDashSeparator_CorrectStructure()
        {
            var result = _generator.GenerateComplexName("{W:2:-}");
            var parts = result.Split('-');
            Assert.That(parts, Has.Length.EqualTo(2));
            Assert.That(parts.All(p => _dictionary.Contains(p)));
        }

        [Test]
        public void Generate_RepeatWordWithUnderscoreSeparator_CorrectStructure()
        {
            var result = _generator.GenerateComplexName("{W:3:_}");
            var parts = result.Split('_');
            Assert.That(parts, Has.Length.EqualTo(3));
            Assert.That(parts.All(p => _dictionary.Contains(p)));
        }

        [Test]
        public void Generate_RepeatWordWithSpaceSeparator_CorrectStructure()
        {
            var result = _generator.GenerateComplexName("{w:2: }");
            var parts = result.Split(' ');
            Assert.That(parts, Has.Length.EqualTo(2));
            Assert.That(parts.All(p => _dictionary.Select(w => w.ToLower()).Contains(p)));
        }

        [Test]
        public void Generate_RepeatWithNoSeparator_BehavesAsBeforeForNonWordTokens()
        {
            var result = _generator.GenerateComplexName("{A:3}");
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result.All(char.IsUpper));
        }

        [Test]
        public void Generate_RepeatWithEmptySeparator_NoSeparatorBetweenWords()
        {
            var result = _generator.GenerateComplexName("{W:2:}");
            Assert.That(_dictionary.Any(w => result.StartsWith(w)));
        }
        #endregion

        #region ── SEQ token ──────────────────────────────────────────────────────

        [Test]
        public void Generate_Seq_DefaultStartsAtOne()
        {
            var result = _generator.GenerateComplexName("{SEQ}");
            Assert.That(result, Is.EqualTo("1"));
        }

        [Test]
        public void Generate_Seq_IncrementsOnSubsequentCalls()
        {
            Assert.That(_generator.GenerateComplexName("{SEQ}"), Is.EqualTo("1"));
            Assert.That(_generator.GenerateComplexName("{SEQ}"), Is.EqualTo("2"));
            Assert.That(_generator.GenerateComplexName("{SEQ}"), Is.EqualTo("3"));
        }

        [Test]
        public void Generate_Seq_CustomStart()
        {
            var result = _generator.GenerateComplexName("{SEQ:5}");
            Assert.That(result, Is.EqualTo("5"));
        }

        [Test]
        public void Generate_Seq_CustomStartIncrements()
        {
            Assert.That(_generator.GenerateComplexName("{SEQ:5}"), Is.EqualTo("5"));
            Assert.That(_generator.GenerateComplexName("{SEQ:5}"), Is.EqualTo("6"));
        }

        [Test]
        public void Generate_Seq_PaddingNoStart()
        {
            Assert.That(_generator.GenerateComplexName("{SEQ::3}"), Is.EqualTo("001"));
            Assert.That(_generator.GenerateComplexName("{SEQ::3}"), Is.EqualTo("002"));
        }

        [Test]
        public void Generate_Seq_PaddingWithStart()
        {
            Assert.That(_generator.GenerateComplexName("{SEQ:5:3}"), Is.EqualTo("005"));
            Assert.That(_generator.GenerateComplexName("{SEQ:5:3}"), Is.EqualTo("006"));
        }

        [Test]
        public void Generate_Seq_PaddingLargeNumber_NoPadTruncation()
        {
            var generator = BuildGenerator(_dictionary, new NameContext());
            for (int i = 0; i < 999; i++)
                generator.GenerateComplexName("{SEQ::3}");
            Assert.That(generator.GenerateComplexName("{SEQ::3}"), Is.EqualTo("1000"));
        }

        [Test]
        public void Generate_Seq_CaseInsensitive()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(_generator.GenerateComplexName("{seq}"), Is.EqualTo("1"));
                Assert.That(_generator.GenerateComplexName("{SEQ}"), Is.EqualTo("2"));
            }
        }

        [Test]
        public void Generate_Seq_InvalidStart_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{SEQ:abc}"));

        [Test]
        public void Generate_Seq_InvalidPad_ThrowsFormatException()
            => Assert.Throws<FormatException>(() => _generator.GenerateComplexName("{SEQ:1:abc}"));
        #endregion

        #region ── Deterministic random ──────────────────────────────────────────

        [Test]
        public void Generate_WithSeededRandom_IsDeterministic()
        {
            var r1 = new Random(42);
            var r2 = new Random(42);

            // Verify seeds produce same sequence before even touching generators
            Assert.That(r1.Next(100), Is.EqualTo(r2.Next(100)));

            var result1 = new NameGenerator(_dictionary, new NameContext(),
                BuildRegistry(), new Random(42)).GenerateComplexName("{W}-{W}");

            var result2 = new NameGenerator(_dictionary, new NameContext(),
                BuildRegistry(), new Random(42)).GenerateComplexName("{W}-{W}");

            Assert.That(result1, Is.EqualTo(result2));
        }

        [Test]
        public void Generate_WithDifferentSeeds_ProduceDifferentResults()
        {
            var generator1 = new NameGenerator(_dictionary, new NameContext(),
                BuildRegistry(), new Random(42));
            var generator2 = new NameGenerator(_dictionary, new NameContext(),
                BuildRegistry(), new Random(99));

            var results1 = Enumerable.Range(0, 10)
                .Select(_ => generator1.GenerateComplexName("{W}")).ToList();
            var results2 = Enumerable.Range(0, 10)
                .Select(_ => generator2.GenerateComplexName("{W}")).ToList();

            Assert.That(results1, Is.Not.EqualTo(results2));
        }
        #endregion

        #region ── Custom token provider ─────────────────────────────────────────

        [Test]
        public void Generate_CustomProvider_UsedForToken()
        {
            var registry = BuildRegistry();
            registry.Register(new FixedValueProvider());
            var generator = new NameGenerator(_dictionary, new NameContext(),
                registry, Random.Shared);

            var result = generator.GenerateComplexName("{FIXED}");
            Assert.That(result, Is.EqualTo("fixed-value"));
        }

        [Test]
        public void Generate_CustomProviderOverridesBuiltin_LastWins()
        {
            var registry = BuildRegistry();
            registry.Register(new OverrideWordProvider());
            var generator = new NameGenerator(_dictionary, new NameContext(),
                registry, Random.Shared);

            // {W} should now return "override" instead of a dictionary word
            var result = generator.GenerateComplexName("{W}");
            Assert.That(result, Is.EqualTo("override"));
        }

        private class FixedValueProvider : ITokenProvider
        {
            public bool CanHandle(string token) =>
                token.Equals("FIXED", StringComparison.OrdinalIgnoreCase);

            public string Generate(TokenContext ctx) => "fixed-value";
        }

        private class OverrideWordProvider : ITokenProvider
        {
            public bool CanHandle(string token) => token == "W";
            public string Generate(TokenContext ctx) => "override";
        }
        #endregion
    }
}