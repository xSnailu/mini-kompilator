
using System;
using System.IO;
using System.Collections.Generic;


public enum CompilerType { Int_Type, Bool_Type, Double_type, Hex_Type, Void_Type}

public class Compiler
{

    public static int errors = 0;

    public static StatementNode rootNode;

    public static void SetRoot(StatementNode root)
    {
        rootNode = root;
    }

    public static List<string> source;

    // arg[0] określa plik źródłowy
    // pozostałe argumenty są ignorowane
    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nLLVM Code Generator for MiNI language - Gardens Point Tools");
        if (args.Length >= 1)
            file = args[0];
        else
        {
            Console.Write("\nsource file:  ");
            file = Console.ReadLine();
        }
        try
        {
            var sr = new StreamReader(file);
            string str = sr.ReadToEnd();
            sr.Close();
            Compiler.source = new System.Collections.Generic.List<string>(str.Split(new string[] { "\r\n" }, System.StringSplitOptions.None));
            source = new FileStream(file, FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
            return 1;
        }
        Scanner scanner = new Scanner(source);
        Parser parser = new Parser(scanner);
        Console.WriteLine();
        parser.Parse();
        source.Close();
        if (errors == 0)
        {
            sw = new StreamWriter(file + ".ll");
            rootNode.GenCode();
            sw.Close();
            Console.WriteLine("  compilation successful\n");
        }
        else
            Console.WriteLine($"\n  {errors} errors detected\n");
        return errors == 0 ? 0 : 2;
    }

    public static void EmitCode(string instr = null)
    {
        sw.WriteLine(instr);
    }

    public static void EmitCode(string instr, params object[] args)
    {
        sw.WriteLine(instr, args);
    }

    public static string NewTemp()
    {
        return string.Format($"%t{++nr}");
    }

    private static StreamWriter sw;
    private static int nr;

    private static void GenCode()
    {
        EmitCode("; prolog");
        EmitCode("@int_res = constant [15 x i8] c\"  Result:  %d\\0A\\00\"");
        EmitCode("@double_res = constant [16 x i8] c\"  Result:  %lf\\0A\\00\"");
        EmitCode("@end = constant [20 x i8] c\"\\0AEnd of execution\\0A\\0A\\00\"");
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode("define void @main()");
        EmitCode("{");
        for (char c = 'a'; c <= 'z'; ++c)
        {
            EmitCode($"%i{c} = alloca i32");
            EmitCode($"store i32 0, i32* %i{c}");
            EmitCode($"%r{c} = alloca double");
            EmitCode($"store double 0.0, double* %r{c}");
        }
        EmitCode();

        for (int i = 0; i < code.Count; ++i)
        {
            EmitCode($"; linia {i + 1,3} :  " + source[i]);
            code[i].GenCode();
            EmitCode();
        }
        EmitCode("}");
    }
}

public abstract class SyntaxTreeNode
{
    public CompilerType type;
    public int line = -1;
    public abstract CompilerType CheckType();
    public abstract string GenCode();
}

public class StatementNode : SyntaxTreeNode
{
    List<SyntaxTreeNode> leftChild;
    List<SyntaxTreeNode> rightChild;

    public StatementNode() { leftChild = new List<SyntaxTreeNode>(); rightChild = new List<SyntaxTreeNode>(); }
    public StatementNode(StatementNode lC, StatementNode rC)
    {
        this.leftChild = new List<SyntaxTreeNode>(lC.leftChild);
        this.leftChild.AddRange(lC.rightChild);

        this.rightChild = new List<SyntaxTreeNode>(rC.leftChild);
        this.rightChild.AddRange(rC.rightChild);

    }   
    public StatementNode(StatementNode lC, ExpressionNode eR)
    {
        this.leftChild = new List<SyntaxTreeNode>(lC.leftChild);
        this.leftChild.AddRange(lC.leftChild);

        this.rightChild.Add(eR);

    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        List<SyntaxTreeNode> cummulatedNodes = new List<SyntaxTreeNode>(this.leftChild);
        cummulatedNodes.AddRange(this.rightChild);

        foreach(var node in cummulatedNodes)
        {
            node.GenCode();
        }

        return null;
    }
}

public abstract class ExpressionNode : SyntaxTreeNode
{
    
}

// DECL
public class DeclarationNode : StatementNode
{
    string ident;
    public DeclarationNode(string typeS, string i)
    {
        ident = i;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}


// STMT_IF
public class IfNode : StatementNode
{
    StatementNode statement;
    ExpressionNode condition;
    public IfNode(StatementNode s, ExpressionNode c)
    {
        statement = s;
        condition = c;
    }

    public override string GenCode()
    {
        return null;
    }
}

public class IfElseNode : StatementNode
{
    StatementNode statementT;
    StatementNode statementF;
    ExpressionNode condition;

    public IfElseNode(StatementNode sT, StatementNode sF, ExpressionNode c)
    {
        statementT = sT;
        statementF = sF;
        condition = c;
    }

    public override string GenCode()
    {
        return null;
    }
}

// STMT_WHILE

public class WhileNode : StatementNode
{
    StatementNode statement;
    ExpressionNode condition;

    public WhileNode(StatementNode s, ExpressionNode c)
    {
        statement = s;
        condition = c;
    }

    public override string GenCode()
    {
        return null;
    }
}

// STMT_WRITE
public class WriteStringNode : StatementNode
{
    string str;

    public WriteStringNode(string s)
    {
        str = s;
    }

    public override string GenCode()
    {
        return null;
    }
}

public class WriteIdentNode : StatementNode
{
    string ident;

    public WriteIdentNode(string i)
    {
        ident = i;
    }

    public override string GenCode()
    {
        return null;
    }
}
public class WriteHexIdentNode : StatementNode
{
    string ident;

    public WriteHexIdentNode(string i)
    {
        ident = i;
    }

    public override string GenCode()
    {
        return null;
    }
}

public class WriteExpressionNode : StatementNode
{
    ExpressionNode expression;

    public WriteExpressionNode(ExpressionNode e)
    {
        expression = e;
    }

    public override string GenCode()
    {
        return null;
    }
}
// STMT_READ
public class ReadNode : StatementNode
{
    string ident;

    public ReadNode(string i)
    {
        this.ident = i;
    }

    public override string GenCode()
    {
        return null;
    }
}

public class ReadHexNode : StatementNode
{
    string ident;

    public ReadHexNode(string i)
    {
        this.ident = i;
    }

    public override string GenCode()
    {
        return null;
    }
}

// STMT_RETURN
public class ReturnNode : StatementNode
{
    public ReturnNode() { }
    public override string GenCode()
    {
        return null;
    }
}

public class AssignNode : ExpressionNode
{
    string ident;
    ExpressionNode expression;

    public AssignNode(string i, ExpressionNode e)
    {
        ident = i;
        expression = e;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

// UNARY_EXPR
public class ValueNode : ExpressionNode
{
    string value;

    public ValueNode(string v)
    {
        value = v;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

public class IdentNode : ExpressionNode
{
    string ident;

    public IdentNode(string i)
    {
        ident = i;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

public class ConvertToNode : ExpressionNode
{
    ExpressionNode expression;
    public ConvertToNode(ExpressionNode e)
    {
        expression = e;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

public class BitwiseNegationNode : ExpressionNode
{
    ExpressionNode expression;
    public BitwiseNegationNode(ExpressionNode e)
    {
        expression = e;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

public class LogicalNegationNode : ExpressionNode
{
    ExpressionNode expression;
    public LogicalNegationNode(ExpressionNode e)
    {
        expression = e;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

public class UnaryMinusNode : ExpressionNode
{
    ExpressionNode expression;
    public UnaryMinusNode(ExpressionNode e)
    {
        expression = e;
    }
    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        return null;
    }
}

// BITWISE_EXPR
public class BitwiseOrNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public BitwiseOrNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class BitwiseAndNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public BitwiseAndNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

// MULTIPLICATIVE_EXPR
public class MultipliesNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public MultipliesNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class DivisionNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public DivisionNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

// ADDITIVE_EXPR
public class PlusNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public PlusNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class MinusNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public MinusNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

// RELATIONAL_EXPR
public class EqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public EqualNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class UnequalNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public UnequalNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class GreaterNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public GreaterNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class GreaterOrEqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public GreaterOrEqualNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class LessNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LessNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class LessOrEqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LessOrEqualNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

// LOGICAL_EXPR
public class LogicalAndNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LogicalAndNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}

public class LogicalOrNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LogicalOrNode(ExpressionNode lE, BitwiseOrNode rE)
    {
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        throw new NotImplementedException();
    }

    public override string GenCode()
    {
        throw new NotImplementedException();
    }
}
class ErrorException : ApplicationException
{
    public readonly bool Recovery;
    public ErrorException(bool rec = true) { ++Compiler.errors; Recovery = rec; }
    public ErrorException(string msg, bool rec = true) : base(msg) { ++Compiler.errors; Recovery = rec; }
    public ErrorException(string msg, Exception ex, bool rec = true) : base(msg, ex) { ++Compiler.errors; Recovery = rec; }
}