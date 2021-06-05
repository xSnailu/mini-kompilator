%using QUT.Gppg;
%namespace GardensPoint

IntNumber     (0|[1-9][0-9]*)
HexIntNumber  (0(x|X)([a-f0-9]+))
RealNumber    (0|[1-9][0-9]*)\.[0-9]+
Ident         [a-zA-z][a-zA-Z0-9]*
Comment       \/\/.*$
Str        \"(\\.|[^"\n\\])*\"

%%

"program"		{ return (int)Tokens.Program; }
"if"			{ return (int)Tokens.If; }
"else"			{ return (int)Tokens.Else; }
"while" 		{ return (int)Tokens.While; }
"read" 			{ return (int)Tokens.Read; }
"write"			{ return (int)Tokens.Write; }
"return"		{ return (int)Tokens.Return; }
"int"			{ return (int)Tokens.Int; }
"double"		{ return (int)Tokens.Double; }
"bool"			{ return (int)Tokens.Bool; }
"true"			{ return (int)Tokens.True; }
"false" 		{ return (int)Tokens.False; }
"hex" 		    { return (int)Tokens.Hex; }	

"="             { return (int)Tokens.Assign; }
"||"            { return (int)Tokens.LogOr; }
"&&"            { return (int)Tokens.LogAnd; }
"|"             { return (int)Tokens.BitOr; }
"&"             { return (int)Tokens.BitAnd; }
"=="            { return (int)Tokens.Equal; }
"!="            { return (int)Tokens.Unequal; }
">"             { return (int)Tokens.Greater; }
">="            { return (int)Tokens.GreaterOrEqual; }
"<"             { return (int)Tokens.Less; }
"<="            { return (int)Tokens.LessOrEqual; }
"+"             { return (int)Tokens.Plus; }
"-"             { return (int)Tokens.Minus; }
"*"             { return (int)Tokens.Multiplies; }
"/"             { return (int)Tokens.Division; }
"!"             { return (int)Tokens.LogNegation; }
"~"             { return (int)Tokens.BitNegation; }
"("             { return (int)Tokens.LeftBracket; }
")"             { return (int)Tokens.RightBracket; }
"{"             { return (int)Tokens.LeftCurlyBracket; }
"}"             { return (int)Tokens.RightCurlyBracket; }
","             { return (int)Tokens.Comma; }
";"             { return (int)Tokens.Semicolon; }

"\r\n"			{ Compiler.lineno++; }
"\r"            { }
"\t"            { }
" "             { }
<<EOF>>       	{ return (int)Tokens.Eof; }

{IntNumber}   	    { yylval.value=yytext; return (int)Tokens.IntNumber; }
{HexIntNumber}   	{ yylval.value=yytext; return (int)Tokens.IntNumber; }
{RealNumber}		{ yylval.value=yytext; return (int)Tokens.RealNumber; }
{Ident}       	    { yylval.value=yytext; return (int)Tokens.Ident; }
{Str}				{ yylval.value=yytext; return (int)Tokens.Str; }
{Comment}		    { Compiler.lineno++; }

.					{ return (int)Tokens.error; }