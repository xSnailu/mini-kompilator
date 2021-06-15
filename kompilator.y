%namespace GardensPoint

%union
{
public string                               value;
public SyntaxTreeExpressionNode             EXPnode;
public SyntaxTreeNode                       STMTnode;
}

%token Program If Else While Read Write Return Int Double Bool Hex Eof Error
%token Assign LogOr LogAnd BitOr BitAnd Equal Unequal Greater GreaterOrEqual Less LessOrEqual Plus Minus Multiplies Division LogNegation BitNegation LeftBracket RightBracket LeftCurlyBracket RightCurlyBracket Comma Semicolon

%token <value>   IntNumber RealNumber Ident Str True False Type HexIntNumber
%type <EXPnode>  EXPR LOGICAL_EXPR RELATIONAL_EXPR ADDITIVE_EXPR MULTIPLICATIVE_EXPR BITWISE_EXPR UNARY_EXPR TERMINAL
%type <STMTnode> STMT_LIST STMT CODE_BLOCK STMT_RETURN STMT_WRITE STMT_READ STMT_IF STMT_WHILE DECL_COMMA_BOOL DECL_COMMA_INT DECL_COMMA_DOUBLE
%type <STMTnode> MAIN_CODE_BLOCK DECL_LIST DECL

%%

START:          Program MAIN_CODE_BLOCK Eof
                {
                    Compiler.rootNode = $2;
                }
|               Program error Eof
                {
                    Console.WriteLine("SYNTAX ERROR. LINE: " + Compiler.lineno);
                    Compiler.errors++;
                }
|               Program LeftBracket DECL_LIST STMT_LIST Eof
                {
                    Console.WriteLine("SYNTAX ERROR. LINE: " + Compiler.lineno);
                    Compiler.errors++;
                }
|               Eof
                {
                    Console.WriteLine("SYNTAX ERROR. LINE: " + Compiler.lineno);
                    Compiler.errors++;
                }
;

MAIN_CODE_BLOCK:LeftCurlyBracket DECL_LIST STMT_LIST RightCurlyBracket
                {
                    $$ = new SyntaxTreeStatementNode($2, $3); 
                }
|               LeftCurlyBracket DECL_LIST STMT_LIST Eof
                {
                    Console.WriteLine("SYNTAX ERROR. LINE: " + Compiler.lineno);
                    Compiler.errors++;
                }
;

CODE_BLOCK:  LeftCurlyBracket STMT_LIST RightCurlyBracket
                    {
                    $$ = $2;
                    }
|                   LeftCurlyBracket STMT_LIST Eof
                    {
                        Console.WriteLine("SYNTAX ERROR. LINE: " + Compiler.lineno);
                        Compiler.errors++;
                    }
;

DECL_LIST:      DECL_LIST  DECL
                {
                       $$ = new SyntaxTreeStatementNode($1, $2);
                }
|               
                {
                        $$ = new SyntaxTreeStatementNode();
                }
;

DECL:           Bool Ident Semicolon
                {
                $$ = new DeclarationNode("bool", $2);
                }
|               Int Ident Semicolon
                {
                $$ = new DeclarationNode("int", $2);
                }
|               Double Ident Semicolon
                {
                $$ = new DeclarationNode("double", $2);
                }
|               Bool Ident DECL_COMMA_BOOL Semicolon 
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("bool", $2), $3);
                }
|               Int Ident DECL_COMMA_INT Semicolon
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("int", $2), $3);
                }
|               Double Ident DECL_COMMA_DOUBLE Semicolon
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("double", $2), $3);
                }
;

DECL_COMMA_BOOL: Comma Ident DECL_COMMA_BOOL
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("bool", $2), $3);
                }
|               Comma Ident
                {
                $$ = new SyntaxTreeStatementNode(new SyntaxTreeStatementNode(), new DeclarationNode("bool", $2));
                }
;

DECL_COMMA_DOUBLE: Comma Ident DECL_COMMA_DOUBLE
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("double", $2), $3);
                }
|               Comma Ident
                {
                $$ = new SyntaxTreeStatementNode(new SyntaxTreeStatementNode(), new DeclarationNode("double", $2));;
                }
;

DECL_COMMA_INT: Comma Ident DECL_COMMA_INT
                {
                $$ = new SyntaxTreeStatementNode(new DeclarationNode("int", $2), $3);
                }
|               Comma Ident
                {
                $$ = new SyntaxTreeStatementNode(new SyntaxTreeStatementNode(), new DeclarationNode("int", $2));
                }
;

STMT_LIST:  STMT_LIST STMT
            {
                $$ = new SyntaxTreeStatementNode($1, $2);
            }
|           
            {
                $$ = new SyntaxTreeStatementNode();
            }
;

STMT:       STMT_IF
            {
            $$ = $1;
            }
|           STMT_WRITE
            {
            $$ = $1;
            }
|           STMT_READ
            {
            $$ = $1;
            }          
|           STMT_WHILE
            {
            $$ = $1;
            }
|           STMT_RETURN
            {
            $$ = $1;
            }
|           CODE_BLOCK
            {
            $$ = $1;
            }
|           EXPR Semicolon
            {
            $$ = new ExpressionParentNode($1);
            }
;

STMT_IF:        If LeftBracket EXPR RightBracket STMT Else STMT
                {
                $$ = new IfElseNode($5, $7, $3);
                }
|               If LeftBracket EXPR RightBracket STMT
                {
                $$ = new IfNode($5, $3);
                }
;

STMT_WRITE: 	Write EXPR Comma Hex Semicolon
                {
                $$ = new WriteHexExpressionNode($2);
                }
|               Write EXPR Semicolon
                {
                $$ = new WriteExpressionNode($2);
                }
|               Write Str Semicolon
                {
                $$ = new WriteStringNode($2);
                }
;

STMT_READ:      Read Ident Comma Hex Semicolon
                {
                $$ = new ReadHexNode($2);
                }
|               Read Ident Semicolon
                {
                $$ = new ReadNode($2);
                }
;

STMT_WHILE:     While LeftBracket EXPR RightBracket STMT
                {
                $$ = new WhileNode($5, $3);
                }
;

STMT_RETURN:    Return Semicolon
                {
                $$ = new ReturnNode();
                }
;

EXPR:           Ident Assign EXPR
                {
                $$ = new AssignNode($1, $3);
                }
|               LOGICAL_EXPR
                {
                $$ = $1;
                }
;

LOGICAL_EXPR:   LOGICAL_EXPR LogAnd RELATIONAL_EXPR
                {
                $$ = new LogicalAndNode($1, $3);
                }
|               LOGICAL_EXPR LogOr RELATIONAL_EXPR
                {
                $$ = new LogicalOrNode($1, $3);
                }
|               RELATIONAL_EXPR
                {
                $$ = $1;
                }
;

RELATIONAL_EXPR: RELATIONAL_EXPR Equal ADDITIVE_EXPR
                {
                $$ = new EqualNode($1, $3);
                }
|               RELATIONAL_EXPR Unequal ADDITIVE_EXPR
                {
                $$ = new UnequalNode($1, $3);
                }
|               RELATIONAL_EXPR Greater ADDITIVE_EXPR
                {
                $$ = new GreaterNode($1, $3);
                }
|               RELATIONAL_EXPR GreaterOrEqual ADDITIVE_EXPR
                {
                $$ = new GreaterOrEqualNode($1, $3);
                }
|               RELATIONAL_EXPR Less ADDITIVE_EXPR
                {
                $$ = new LessNode($1, $3);
                }
|               RELATIONAL_EXPR LessOrEqual ADDITIVE_EXPR
                {
                $$ = new LessOrEqualNode($1, $3);
                }
|               ADDITIVE_EXPR
                {
                $$ = $1;
                }
;

ADDITIVE_EXPR:  ADDITIVE_EXPR Plus MULTIPLICATIVE_EXPR
                {
                $$ = new PlusNode($1, $3);
                }
|               ADDITIVE_EXPR Minus MULTIPLICATIVE_EXPR
                {
                $$ = new MinusNode($1, $3);
                }
|               MULTIPLICATIVE_EXPR
                {
                $$ = $1;
                }
;

MULTIPLICATIVE_EXPR: MULTIPLICATIVE_EXPR Multiplies BITWISE_EXPR
                    {
                    $$ = new MultipliesNode($1, $3);
                    }
|                   MULTIPLICATIVE_EXPR Division BITWISE_EXPR
                    {
                    $$ = new DivisionNode($1, $3);
                    }
|                   BITWISE_EXPR
                    {
                    $$ = $1;
                    }
;

BITWISE_EXPR:   BITWISE_EXPR BitOr UNARY_EXPR
                {
                $$ = new BitwiseOrNode($1, $3);
                }
|               BITWISE_EXPR BitAnd UNARY_EXPR
                {
                $$ = new BitwiseAndNode($1, $3);
                }
|               UNARY_EXPR
                {
                $$ = $1;
                }
;

UNARY_EXPR:     TERMINAL
                {
                $$ = $1;
                }
|               Minus UNARY_EXPR
                {
                $$ = new UnaryMinusNode($2);
                }
|               LogNegation UNARY_EXPR
                {
                $$ = new LogicalNegationNode($2);
                }
|               BitNegation UNARY_EXPR
                {
                $$ = new BitwiseNegationNode($2);
                }
|               LeftBracket Int RightBracket UNARY_EXPR
                {
                $$ = new ConvertToNode("int", $4);
                }
|               LeftBracket Double RightBracket UNARY_EXPR
                {
                $$ = new ConvertToNode("double", $4);
                }
|               LeftBracket EXPR RightBracket
                {
                $$ = $2;
                }
;

TERMINAL:       IntNumber
                {
                $$ = new ValueNode("int", $1);
                }
|               RealNumber
                {
                $$ = new ValueNode("double", $1);
                }
|               HexIntNumber
                {
                $$ = new ValueNode("hex", $1);
                }
|               True
                {
                $$ = new ValueNode("bool", "true");
                }
|               False
                {
                $$ = new ValueNode("bool", "false");
                }
|               Ident
                {
                $$ = new IdentNode($1);
                }
;
%%

public Parser(Scanner scanner) : base(scanner) { }