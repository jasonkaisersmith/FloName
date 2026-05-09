# FloName

A flexible .NET library for generating random, structured filenames using a powerful format string syntax.

## Features

- Format string based filename generation with rich token support
- Word-based generation using language-specific dictionaries
- Sequential numbering with padding
- Date/time tokens using full .NET format string support
- Uniqueness enforcement within a single generation call
- Batch generation with shared sequence state
- Extensible token provider architecture
- Fluent builder API
- REST API webserver
- Console application

## Projects

| Project | Description |
|---------|-------------|
| `FloName` | Core library |
| `FloName.Api` | ASP.NET Core REST API |
| `FloNameConsole` | Console application |
| `Test_FloName` | NUnit test suite |

## Quick Start

### Library

```csharp
var generator = new FilenameGenerator("path/to/dicts");

// Single file — fluent API
var name = generator.For("en")
                    .WithFormat("{W}-{W}-{N:4}")
                    .WithExtension(".txt")
                    .Generate();
// → River-Table-3847.txt

// Batch generation
var names = generator.For("en")
                     .WithFormat("{W}-{W}")
                     .WithExtension(".txt")
                     .GenerateBatch(10);

// Simple word-based name
var simple = generator.For("en")
                      .WithExtension(".txt")
                      .GenerateSimple(wordCount: 3, separator: '-');
// → river-table-stone.txt
```



### REST API

```bash
# Single filename
GET /generate/en?format={W}-{W}&extension=.txt

# Batch
GET /batch/en?format={W}-{W}&count=10&extension=.txt

# Supported languages
GET /languages

# Token reference
GET /help

# Health check
GET /health
```

### Console

```bash
floname generate en --format "{W}-{W}" --extension .txt
floname batch en --format "{W}-{W}" --count 10 --table
floname languages
floname tokens
```

## Format String Reference

Tokens are written as `{token}` inside a format string. Literal characters outside braces are passed through as-is.

### Tokens

| Token | Description | Example |
|-------|-------------|---------|
| `{a}` | Single lowercase letter (a–z) | `{a}` → k |
| `{A}` | Single uppercase letter (A–Z) | `{A}` → K |
| `{n}` | Single digit (0–9) | `{n}` → 4 |
| `{N}` | Single non-zero digit (1–9) | `{N}` → 7 |
| `{an}` | Single lowercase letter or digit | `{an}` → k4 |
| `{An}` | Single uppercase letter or digit | `{An}` → K4 |
| `{AN}` / `{aN}` | Single uppercase letter or non-zero digit | `{AN}` → K4 |
| `{w}` | Random word, lowercase | `{w}` → river |
| `{W}` | Random word, original casing | `{W}` → River |
| `{SEQ}` | Sequential counter | `{SEQ}` → 1, 2, 3 |
| `{DATE:format}` | Current date/time (.NET format string) | `{DATE:yyyy-MM-dd}` → 2026-05-08 |

### Modifiers

| Modifier | Description | Example |
|----------|-------------|---------|
| `:N` | Repeat token N times | `{A:4}` → KXRM |
| `:N:sep` | Repeat with separator | `{W:2:-}` → River-Table |
| `U` | Unique within this generation call | `{AU}` → unique letter |
| `{SEQ:start}` | SEQ with custom start | `{SEQ:5}` → 5, 6, 7 |
| `{SEQ::pad}` | SEQ with padding | `{SEQ::3}` → 001, 002 |
| `{SEQ:start:pad}` | SEQ with start and padding | `{SEQ:5:3}` → 005, 006 |

### Examples
{W}-{W}-{N:4}                       → River-Table-3847
{date:yyyyMMdd}-{A:4}               → 20260508-KXRM
{w}-{nU}{nU}{nU}                    → forest-492
report_{DATE:yyyy-MM-dd}_{A:2}{n:3} → report_2026-05-08-XK291
{W:3:-}                             → River-Table-Stone
{SEQ:1:3}-{W}                       → 001-River

## Dictionaries

Dictionaries are JSON files in the `dicts` folder, named `dictionary_{lang}.json`:
Any .json file in this directory will be read into the dictionaries.  It must end in _{lang}.
e.g.
dictionary_en.json

```json
["apple", "river", "table", "forest", "stone", "cloud"]
```

Supported out of the box: `en`, `de`, `fr`, `es`, `pt`. Add any language by dropping in a new dictionary file.


### Custom Token Provider
You can extend the built in Token providers by adding your own.  Example below.
```csharp
public class GuidTokenProvider : ITokenProvider
{
    public bool CanHandle(string token) =>
        token.Equals("GUID", StringComparison.OrdinalIgnoreCase);

    public string Generate(TokenContext ctx) =>
        Guid.NewGuid().ToString("N")[..8];
}

var generator = new FilenameGenerator("dicts")
    .RegisterProvider(new GuidTokenProvider());

var name = generator.For("en")
                    .WithFormat("{GUID}-{W}")
                    .Generate();
// → a3f2b1c4-River.txt
```

## Running the API

### Local

```bash
cd FloName.Api
dotnet run
```

### Docker

The `dicts` folder is mounted as a volume — add new dictionaries without rebuilding the image.

Configure the dicts path via environment variable:

```bash
FloName__DictsPath=/app/dicts
```

## Requirements

- .NET 10
- Docker for API (optional)

## License

MIT
