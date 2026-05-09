using System;
using System.Collections.Generic;
using System.Text;

namespace FloName
{
    public record TokenInfo(string Token, string Description, string Example);

    public record HelpResponse(
        string Description,
        IReadOnlyList<TokenInfo> Tokens,
        IReadOnlyList<TokenInfo> Modifiers,
        IReadOnlyList<string> Examples
    );
}
