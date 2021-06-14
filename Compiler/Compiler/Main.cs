
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Text.RegularExpressions;

public enum CompilerType { Int_Type, Bool_Type, Double_type, Hex_Type, Void_Type}

public class Compiler
{

    public static int errors = 0;

    public static int lineno = -1;

    public static int syntaxErrors = 0;

    public static SyntaxTreeNode rootNode;

    public static Dictionary<string, CompilerType> Variables = new Dictionary<string, CompilerType>();
    public static List<string> Strings = new List<string>();

    public static List<string> LogicalOperationsIdents = new List<string>();

    public static int compilerCondIndex = 0;
    public static void SetRoot(SyntaxTreeNode root)
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
            //file = "test.txt";
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



        // wykrywanie błędów np. ponowna deklaracja albo uzycie niezadeklarowanej zmiennej
        try
        {
            parser.Parse();
        }
        catch (CompilerException e)
        {
            Console.WriteLine(e.Message);
            return 1;
        }
        source.Close();

        if (errors > 0)
        {
            return 1;
        }

        // sprawdzanie typów
        if (rootNode == null)
        {
            Console.WriteLine("Parser can't parse program.");
            return 1;
        }

        try
        {
            rootNode.CheckType();
        }
        catch (CompilerException e)
        {
            Console.WriteLine(e.Message);
            return 1;
        }

        // generowanie kodu
        sw = new StreamWriter(file + ".ll");
        GenProlog();
        try
        {
            if (rootNode == null)
            {
                Console.WriteLine("UNEXPECTED ERROR");
                Console.WriteLine("Parser can't parse program.");
                return 1;
            }
            else
            {
                GenCode();
            }
        }
        catch (CompilerException e)
        {
            Console.WriteLine("UNEXPECTED ERROR");
            Console.WriteLine(e.Message);
            return 1;
        }

        GenEpilog();
        sw.Close();
        Console.WriteLine("  compilation successful\n");
        return 0;
    }

    public static void EmitCode(string instr = null)
    {
        sw.WriteLine(instr);
    }

    public static void EmitCode(string instr, params object[] args)
    {
        sw.WriteLine(instr, args);
    }

    private static StreamWriter sw;

    private static void GenCode()
    {
        rootNode.GenCode();
    }

    private static void GenProlog()
    {
        EmitCode("; prolog");
        for(int i = 0; i < Strings.Count; i++)
        {
            string strName = getStringGlobalVariableNameById(i);
            string str = Strings[i];
            int specialCharCount = Regex.Matches(str, @"\\[0-9 A-Z][0-9 A-F]").Count;
            EmitCode($"{strName} = constant [{str.Length + 1 - specialCharCount * 2} x i8] c\"{str}\\00\"");
        }
        EmitCode("@int_res = constant [3 x i8] c\"%d\\00\"");
        EmitCode("@double_res = constant [4 x i8] c\"%lf\\00\"");
        EmitCode("@hex_res = constant [5 x i8] c\"0X%X\\00\"");
        EmitCode("@true_res = constant [5 x i8] c\"True\\00\"");
        EmitCode("@false_res = constant [6 x i8] c\"False\\00\"");
        EmitCode("@read_int = constant [3 x i8] c\"%d\\00\"");
        EmitCode("@read_double = constant [4 x i8] c\"%lf\\00\"");
        EmitCode("@read_hex= constant [3 x i8] c\"%X\\00\"");
        EmitCode("declare i32 @printf(i8*, ...)");
        EmitCode("declare i32 @scanf(i8*, ...)");
        EmitCode("define i32 @main()");
        EmitCode("{");
        foreach(var logOpIdent in LogicalOperationsIdents)
        {
            EmitCode($"{logOpIdent} = alloca i1");
        }
        
    }

    private static void GenEpilog()
    {
        EmitCode($"ret i32 0");
        EmitCode("}");
    }

    public static CompilerType stringToCompilerType(string t)
    {
        switch (t)
        {
            case "int":
                return CompilerType.Int_Type;
            case "double":
                return CompilerType.Double_type;
            case "bool":
                return CompilerType.Bool_Type;
            case "hex":
                return CompilerType.Hex_Type;
            default:
                return CompilerType.Void_Type;
        }
    }

    public static string compilerTypeToString(CompilerType t)
    {
        switch (t)
        {
            case CompilerType.Int_Type:
                return "int";
            case CompilerType.Double_type:
                return "double";
            case CompilerType.Bool_Type:
                return "bool";
            case CompilerType.Hex_Type:
                return "hex";
            default:
                return "void";
        }
    }

    public static int tempValueIndex = 0;
    public static string NewValueIdent()
    {
        return string.Format($"%TEMP_VAL{++tempValueIndex}");
    }

    public static int tempPtrIndex = 0;
    public static string newPtrIdent()
    {
        return string.Format($"%TEMP_PTR{++tempPtrIndex}");
    }

    public static int logicalPtrIndex = 0;
    public static string newLogicalPtrIdent()
    {
        return string.Format($"%LOGICAL_OP_PTR{++tempPtrIndex}");
    }

    public static int trueLabelIndex = 0;
    public static string newTrueLabel()
    {
        return string.Format($"TRUE_LABEL{++trueLabelIndex}");
    }

    public static int falseLabelIndex = 0;
    public static string newFalseLabel()
    {
        return string.Format($"FALSE_LABEL{++falseLabelIndex}");
    }

    public static int entryLabelIndex = 0;
    public static string newEntryLabel()
    {
        return string.Format($"ENTRY_LABEL{++entryLabelIndex}");
    }

    public static int outLabelIndex = 0;
    public static string newOutLabel()
    {
        return string.Format($"OUT_LABEL{++outLabelIndex}");
    }

    public static string getStringGlobalVariableNameById(int id)
    {
        return string.Format($"@globalString{++id}");
    }

    public static void addNewError(string errorMsg)
    {
        throw new CompilerException(errorMsg);
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
    SyntaxTreeNode leftNode;
    SyntaxTreeNode rightNode;

    public StatementNode()
    {
        this.line = Compiler.lineno;
        this.type = CompilerType.Void_Type;

        leftNode = new EmptyNode();
        rightNode = new EmptyNode();
    }
    public StatementNode(SyntaxTreeNode lN, SyntaxTreeNode rN)
    {  
        leftNode = lN;
        rightNode = rN;
    }   

    public override CompilerType CheckType()
    {
        if(leftNode != null && !(leftNode is EmptyNode))
        {
            leftNode.CheckType();
        }
        
        if(rightNode != null && !(rightNode is EmptyNode))
        {
            rightNode.CheckType();
        }

        return CompilerType.Void_Type;
    }

    public override string GenCode()
    {
        if (leftNode != null)
        {
            leftNode.GenCode();
        }

        if (rightNode != null)
        {
            rightNode.GenCode();
        }

        return null;
    }
}

public class EmptyNode : SyntaxTreeNode
{
    public EmptyNode()
    {
        this.line = Compiler.lineno;
    }
    public override CompilerType CheckType()
    {
        return CompilerType.Void_Type;
    }
    public override string GenCode()
    {
        //Compiler.EmitCode("%nop = add i1 0, 0");
        return null;
    }
}

public abstract class ExpressionNode : SyntaxTreeNode
{
    
}

public class ExpressionParentNode : SyntaxTreeNode
{
    ExpressionNode expression;

    public ExpressionParentNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        type = CompilerType.Void_Type;
    }
    public override CompilerType CheckType()
    {
        return expression.CheckType();
    }

    public override string GenCode()
    {   
        expression.GenCode();
        return null;
    }
}

// DECL
public class DeclarationNode : SyntaxTreeNode
{
    string ident;
    public DeclarationNode(string typeS, string i)
    {
        this.line = Compiler.lineno;
        ident = i;
        this.type = Compiler.stringToCompilerType(typeS);

        if (Compiler.Variables.ContainsKey(ident))
        {
            Compiler.addNewError($"Variable {ident} already declared. Line: {this.line}");
        }
        else
        {
            Compiler.Variables.Add(ident, this.type);
        }
    }

    public override CompilerType CheckType()
    {
        return this.type;
    }

    public override string GenCode()
    {
        switch (type)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode($"%{this.ident} = alloca i32");
                Compiler.EmitCode($"store i32 0, i32* %{this.ident}");
                break;
            case CompilerType.Bool_Type:
                Compiler.EmitCode($"%{this.ident} = alloca i1");
                Compiler.EmitCode($"store i1 0, i1* %{this.ident}");
                break;
            case CompilerType.Double_type:
                Compiler.EmitCode($"%{this.ident} = alloca double");
                Compiler.EmitCode($"store double 0.0, double* %{this.ident}");
                break;
            case CompilerType.Hex_Type:
                Compiler.EmitCode($"%{this.ident} = alloca i32");
                Compiler.EmitCode($"store i32 0, i32* %{this.ident}");
                break;
            case CompilerType.Void_Type:
                break;
            default:
                break;
        }     
        return null;
    }
}


// STMT_IF
public class IfNode : SyntaxTreeNode
{
    SyntaxTreeNode statement;
    ExpressionNode condition;
    public IfNode(SyntaxTreeNode s, ExpressionNode c)
    {
        this.line = Compiler.lineno;
        statement = s;
        condition = c;
        type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        statement.CheckType();
        CompilerType typeToCheck = condition.CheckType();
        if (typeToCheck != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in if condition at line: {this.line}. Expected bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return CompilerType.Void_Type;
    }

    public override string GenCode()
    {
        string entryLabel = Compiler.newEntryLabel();
        string condTrueLabel = Compiler.newTrueLabel();
        string condFalseLabel = Compiler.newFalseLabel();

        Compiler.EmitCode($"br label %{entryLabel}");
        Compiler.EmitCode($"{entryLabel}:");
        string condIndent = condition.GenCode();
        Compiler.EmitCode($"br i1 {condIndent}, label %{condTrueLabel}, label %{condFalseLabel}");
        Compiler.EmitCode($"{condTrueLabel}:");
        statement.GenCode();
        Compiler.EmitCode($"br label %{condFalseLabel}");
        Compiler.EmitCode($"{condFalseLabel}:");
        return null;
    }
}

public class IfElseNode : SyntaxTreeNode
{
    SyntaxTreeNode statementT;
    SyntaxTreeNode statementF;
    ExpressionNode condition;

    public IfElseNode(SyntaxTreeNode sT, SyntaxTreeNode sF, ExpressionNode c)
    {
        this.line = Compiler.lineno;
        statementT = sT;
        statementF = sF;
        condition = c;
    }

    public override CompilerType CheckType()
    {
        statementF.CheckType();
        statementT.CheckType();

        CompilerType typeToCheck = condition.CheckType();
        if (typeToCheck != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in ifelse condition at line: {this.line}. Expected bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return CompilerType.Void_Type;
    }

    public override string GenCode()
    {
        string entryLabel = Compiler.newEntryLabel();
        string condTrueLabel = Compiler.newTrueLabel();
        string condFalseLabel = Compiler.newFalseLabel();
        string newOutLabel = Compiler.newOutLabel();

        Compiler.EmitCode($"br label %{entryLabel}");
        Compiler.EmitCode($"{entryLabel}:");
        string condIndent = condition.GenCode();
        Compiler.EmitCode($"br i1 {condIndent}, label %{condTrueLabel}, label %{condFalseLabel}");
        Compiler.EmitCode($"{condTrueLabel}:");
        statementT.GenCode();
        Compiler.EmitCode($"br label %{newOutLabel}");
        Compiler.EmitCode($"{condFalseLabel}:");
        statementF.GenCode();
        Compiler.EmitCode($"br label %{newOutLabel}");
        Compiler.EmitCode($"{newOutLabel}:");
        return null;
    }
}

// STMT_WHILE

public class WhileNode : SyntaxTreeNode
{
    SyntaxTreeNode statement;
    ExpressionNode condition;

    public WhileNode(SyntaxTreeNode s, ExpressionNode c)
    {
        this.line = Compiler.lineno;
        statement = s;
        condition = c;
    }

    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = condition.CheckType();
        if (typeToCheck != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in while condition at line: {this.line}. Expected bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return CompilerType.Void_Type;
    }

    public override string GenCode()
    {
        int curCondIndex = Compiler.compilerCondIndex++;
        string entryLabel = Compiler.newEntryLabel();
        string condTrueLabel = Compiler.newTrueLabel();
        string newOutLabel = Compiler.newOutLabel();

        Compiler.EmitCode($"br label %{entryLabel}");
        Compiler.EmitCode($"{entryLabel}:");
        string condIndent = condition.GenCode();
        Compiler.EmitCode($"br i1 {condIndent}, label %{condTrueLabel}, label %{newOutLabel}");
        Compiler.EmitCode($"{condTrueLabel}:");
        statement.GenCode();
        Compiler.EmitCode($"br label %{entryLabel}");
        Compiler.EmitCode($"{newOutLabel}:");
        return null;
    }
}

// STMT_WRITE
public class WriteStringNode : SyntaxTreeNode
{
    int stringId;

    public WriteStringNode(string s)
    {
        this.line = Compiler.lineno;
        String str = s.Substring(1, s.Length - 2); // usuniecie " z konca i poczatku stringa

        // Specjalne znaczenie mają jedynie sekwencje
        // \n -     nowa linia
        // \"    -  wypisanie "
        // \\    -  wypisanie \


        // W pozostałych sytuacjach znak \ jest ignorowany

        try
        {
            for (int i = 0; i + 1 < str.Length; i++)
            {
                if (str.Substring(i, 1) == "\\")
                {
                    var sub = str.Substring(i, 2);
                    if (str.Substring(i, 2) == "\\n" ||
                        str.Substring(i, 2) == "\\\"" ||
                        str.Substring(i, 2) == "\\\\")
                    {
                        i += 1;
                    }
                    else
                    {
                        str = str.Substring(0, i) + str.Substring(i + 1, str.Length - (i + 1));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Problem with parsing string: {0}", s);
        }

        str = str.Replace("\\n",  "\\0A");   // \n    -     nowa linia
        str = str.Replace("\\\"", "\\22");   // \"     -     wypisanie "
        str = str.Replace("\\\\", "\\5C");   // \\     -     wypisanie \

        Compiler.Strings.Add(str); 
        stringId = Compiler.Strings.Count - 1;
        this.type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        return this.type;
    }

    public override string GenCode()
    {
        string str = Compiler.Strings[stringId];
        string name = Compiler.getStringGlobalVariableNameById(stringId);
        int specialCharCount = Regex.Matches(str, @"\\[0-9 A-Z][0-9 A-F]").Count;
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([{str.Length + 1 - specialCharCount * 2} x i8]* {name} to i8*))");
        return null;
    }
}
public class WriteHexExpressionNode : SyntaxTreeNode
{
    ExpressionNode expression;

    public WriteHexExpressionNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        if (typeToCheck != CompilerType.Int_Type)
        {
            Compiler.addNewError($"Wrong argument in hexadecimal write at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {   
        string v = expression.GenCode();
        Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @hex_res to i8*), i32 {v})");
        return null;
    }
}

public class WriteExpressionNode : SyntaxTreeNode
{
    ExpressionNode expression;

    public WriteExpressionNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        if (typeToCheck != CompilerType.Int_Type && typeToCheck != CompilerType.Double_type && typeToCheck != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in write at line: {this.line}. Expected int, double or bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        string v = expression.GenCode();
        CompilerType ExpType = expression.CheckType();
        switch (ExpType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([3 x i8]* @int_res to i8*), i32 {v})");
                break;
            case CompilerType.Bool_Type:
                int curCondIndex = Compiler.compilerCondIndex++;
                string entryLabel = Compiler.newEntryLabel();
                string condTrueLabel = Compiler.newTrueLabel();
                string condFalseLabel = Compiler.newFalseLabel();
                string newOutLabel = Compiler.newOutLabel();
                Compiler.EmitCode($"br label %{entryLabel}");
                Compiler.EmitCode($"{entryLabel}:");
                string condIndent = expression.GenCode();
                Compiler.EmitCode($"br i1 {condIndent}, label %{condTrueLabel}, label %{condFalseLabel}");
                Compiler.EmitCode($"{condTrueLabel}:");
                Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([5 x i8]* @true_res to i8*))");
                Compiler.EmitCode($"br label %{newOutLabel}");
                Compiler.EmitCode($"{condFalseLabel}:");
                Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([6 x i8]* @false_res to i8*))");
                Compiler.EmitCode($"br label %{newOutLabel}");
                Compiler.EmitCode($"{newOutLabel}:");  
                break;
            case CompilerType.Double_type:
                Compiler.EmitCode($"call i32 (i8*, ...) @printf(i8* bitcast ([4 x i8]* @double_res to i8*), double {v})");
                break;
            case CompilerType.Hex_Type:
                break;
            case CompilerType.Void_Type:
                break;
            default:
                break;
        }
        return null;
    }
}
// STMT_READ
public class ReadNode : SyntaxTreeNode
{
    string ident;

    public ReadNode(string i)
    {
        this.line = Compiler.lineno;
        this.ident = i;
        this.type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        if(!Compiler.Variables.ContainsKey(ident))
        {
            Compiler.addNewError($"Read try read to undeclared ident: {this.ident}. LINE: {this.line}.");
        }

        CompilerType typeToCheck = Compiler.Variables[ident];
        if (typeToCheck != CompilerType.Int_Type && typeToCheck != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in read at line: {this.line}. Expected int, double - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        this.type = Compiler.Variables[ident];
        switch (this.type)
        {
            // Uwzględniona konwersja int -> double albo odwrotnie
            case CompilerType.Int_Type:
                Compiler.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([3 x i8]* @read_int to i8*), i32* %{ident})");
                break;
            case CompilerType.Double_type:
                Compiler.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([4 x i8]* @read_double to i8*), double* %{ident})");
                break;
            default:
                break;
        }

        return null;
    }
}

public class ReadHexNode : SyntaxTreeNode
{
    string ident;

    public ReadHexNode(string i)
    {
        this.line = Compiler.lineno;
        this.ident = i;
        this.type = CompilerType.Void_Type;
    }

    public override CompilerType CheckType()
    {
        if (!Compiler.Variables.ContainsKey(ident))
        {
            Compiler.addNewError($"Read try read to undeclared ident: {this.ident}. LINE: {this.line}.");
        }

        CompilerType typeToCheck = Compiler.Variables[ident];
        if (typeToCheck != CompilerType.Int_Type)
        {
            Compiler.addNewError($"Wrong argument in hexadecimal read at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        this.type = Compiler.Variables[ident];
        Compiler.EmitCode($"call i32 (i8*, ...) @scanf(i8* bitcast ([3 x i8]* @read_hex to i8*), i32* %{ident})");
        return null;
    }
}

// STMT_RETURN
public class ReturnNode : SyntaxTreeNode
{
    public ReturnNode()
    {
        this.line = Compiler.lineno;
    }

    public override CompilerType CheckType()
    {
        return CompilerType.Void_Type;
    }

    public override string GenCode()
    {
        Compiler.EmitCode($"ret i32 0");
        return null;
    }
}

public class AssignNode : ExpressionNode
{
    string ident;
    ExpressionNode expression;

    public AssignNode(string i, ExpressionNode e)
    {
        this.line = Compiler.lineno;
        ident = i;
        expression = e;

        if (!Compiler.Variables.ContainsKey(ident))
        {
            Compiler.addNewError($"Can't assign to undeclared ident: {ident}. Line: {this.line}.");
        }

        this.type = Compiler.Variables[ident];
    }
    public override CompilerType CheckType()
    {
        CompilerType identType = Compiler.Variables[ident];
        CompilerType expType = expression.CheckType();

        if (identType == CompilerType.Int_Type)
        {
            if (expType != CompilerType.Int_Type)
            {
                Compiler.addNewError($"Wrong argument in assign at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(expType)}.");
            }
        }

        if(identType == CompilerType.Double_type)
        {
            if (expType != CompilerType.Int_Type && expType != CompilerType.Double_type)
            {
                Compiler.addNewError($"Wrong argument in assign at line: {this.line}. Expected int, double - got {Compiler.compilerTypeToString(expType)}.");
            }
        }

        if (identType == CompilerType.Bool_Type)
        {
            if (expType != CompilerType.Bool_Type)
            {
                Compiler.addNewError($"Wrong argument in assign at line: {this.line}. Expected bool - got {Compiler.compilerTypeToString(expType)}.");
            }
        }

        return this.type;
    }

    public override string GenCode()
    {  
        CompilerType expType = expression.CheckType();

        string v = this.expression.GenCode();
        string assignTempIdent = Compiler.NewValueIdent();
        switch (type)
        {
            case CompilerType.Int_Type: // ident int, arg int (w postaci ident albo value)
                {
                Compiler.EmitCode($"store i32 {v}, i32* %{this.ident}");
                Compiler.EmitCode($"{assignTempIdent} = load i32, i32* %{this.ident}");
                }
                break;
            case CompilerType.Bool_Type: // ident bool, arg bool (w postaci ident albo value)
                {   
                Compiler.EmitCode($"store i1 {v}, i1* %{this.ident}");
                Compiler.EmitCode($"{assignTempIdent} = load i1, i1* %{this.ident}");
                }
                break;
            case CompilerType.Double_type: // ident double, arg int lub double (w postaci ident albo value)
                if (expType == CompilerType.Int_Type) // potrzebna konwersja
                {  
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {v} to double");
                    Compiler.EmitCode($"store double {convertedIdent}, double* %{this.ident}");
                    Compiler.EmitCode($"{assignTempIdent} = load double, double* %{this.ident}");
                }
                else // nie potrzebna konwersja
                {
                    Compiler.EmitCode($"store double {v}, double* %{this.ident}");
                    Compiler.EmitCode($"{assignTempIdent} = load double, double* %{this.ident}");
                }
                break;
            case CompilerType.Hex_Type:
                break;
            case CompilerType.Void_Type:
                break;
            default:
                break;
        }
        return assignTempIdent;
    }
}

// UNARY_EXPR
public class ValueNode : ExpressionNode
{
    string value;

    public ValueNode(string typeS, string v)
    {
        this.line = Compiler.lineno;
        this.value = v;
        this.type = Compiler.stringToCompilerType(typeS);

        switch (this.type)
        {
            case CompilerType.Int_Type:
                break;
            case CompilerType.Bool_Type:
                this.value = this.value == "true" ?  "1" : "0";
                break;
            case CompilerType.Double_type:
                break;
            case CompilerType.Hex_Type:
                this.value = Convert.ToInt32(this.value, 16).ToString();
                this.type = CompilerType.Int_Type;
                break;
            case CompilerType.Void_Type:
                break;
            default:
                break;
        }

    }
    public override CompilerType CheckType()
    {
        return this.type;
    }

    public override string GenCode()
    {
        return this.value;
    }
}

public class IdentNode : ExpressionNode
{
    string ident;

    public IdentNode(string i)
    {
        this.line = Compiler.lineno;
        ident = i;
        // Wszystkie zmienne muszą być zadeklarowane, próba użycia niezadeklarowanej zmiennej
        // powinna powodować błąd kompilacji z komunikatem "undeclared variable"  
        if (!Compiler.Variables.ContainsKey(ident))
        {
            Compiler.addNewError($"An attempt to use undeclared variable {ident}. Line: {this.line}.");
        }
        this.type = Compiler.Variables[ident];
    }
    public override CompilerType CheckType()
    {
        return this.type;
    }

    public override string GenCode()
    {   
        string tempIdent = Compiler.NewValueIdent();
        switch (this.type)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = load {1}, {1}* %{2}", tempIdent, "i32", ident);
                break;
            case CompilerType.Bool_Type:
                Compiler.EmitCode("{0} = load {1}, {1}* %{2}", tempIdent, "i1", ident);
                break;
            case CompilerType.Double_type:
                Compiler.EmitCode("{0} = load {1}, {1}* %{2}", tempIdent, "double", ident);
                break;
            default:
                break;
        }
        return tempIdent;
    }
}

public class ConvertToNode : ExpressionNode
{
    ExpressionNode expression;
    public ConvertToNode(string typeS, ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = Compiler.stringToCompilerType(typeS);
    }
    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        switch (this.type) 
        {
            case CompilerType.Int_Type:
                if (typeToCheck != CompilerType.Int_Type && typeToCheck != CompilerType.Double_type && typeToCheck != CompilerType.Bool_Type)
                {
                    Compiler.addNewError($"Wrong argument in convert to int at line: {this.line}. Expected int, double or bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
                }
                break;
            case CompilerType.Double_type:
                if (typeToCheck != CompilerType.Int_Type && typeToCheck != CompilerType.Double_type)
                {
                    Compiler.addNewError($"Wrong argument in convert to double at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(typeToCheck)}.");
                }
                break;
        }
        return this.type;
    }

    public override string GenCode()
    {
        CompilerType expType = expression.CheckType();
        string curValue = expression.GenCode();
        string bufValue = Compiler.NewValueIdent();
        string convertedValue = Compiler.NewValueIdent();

        switch (this.type)
        {
            case CompilerType.Int_Type:
                if(expType == CompilerType.Int_Type) // nie trzeba konwertowac z int na int
                {
                    convertedValue = curValue;
                    break;
                }else if(expType == CompilerType.Double_type)
                {
                    string newIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{newIdent} = alloca double");
                    Compiler.EmitCode($"store double {curValue}, double* {newIdent}");
                    curValue = newIdent;

                    Compiler.EmitCode($"{bufValue} = load double, double* {curValue}");
                    Compiler.EmitCode($"{convertedValue} = fptosi double {bufValue} to i32");
                    break;
                }else // konwersja z boola
                {
                    Compiler.EmitCode($"{convertedValue} = zext i1 {curValue} to i32 ");
                    break;
                }
            case CompilerType.Double_type:
                if(expType == CompilerType.Double_type) // nie trzeba konwertowac z double na double
                {
                    convertedValue = curValue;
                    break;
                }else
                {
                    string newIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{newIdent} = alloca i32");
                    Compiler.EmitCode($"store i32 {curValue}, i32* {newIdent}");
                    curValue = newIdent;

                    Compiler.EmitCode($"{bufValue} = load i32, i32* {curValue}");
                    Compiler.EmitCode($"{convertedValue} = sitofp i32 {bufValue} to double"); 
                }
                break;
            default:
                break;
        }


        return convertedValue;
    }
}

public class BitwiseNegationNode : ExpressionNode
{
    ExpressionNode expression;
    public BitwiseNegationNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = CompilerType.Int_Type;
    }
    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        if (typeToCheck != CompilerType.Int_Type)
        {
            Compiler.addNewError($"Wrong argument in bitwise negation at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        expression.CheckType();
        string curValue = expression.GenCode();
        string tempValue = Compiler.NewValueIdent();
        Compiler.EmitCode($"{tempValue} = xor i32 {curValue}, -1");
        return tempValue;
    }
}

public class LogicalNegationNode : ExpressionNode
{
    ExpressionNode expression;
    public LogicalNegationNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = CompilerType.Bool_Type;
    }
    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        if (typeToCheck != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in bitwise negation at line: {this.line}. Expected bool - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        string curValue = expression.GenCode();
        string tempValue = Compiler.NewValueIdent();
        Compiler.EmitCode($"{tempValue} = xor i1 {curValue}, -1");
        return tempValue;
    }
}

public class UnaryMinusNode : ExpressionNode
{
    ExpressionNode expression;
    public UnaryMinusNode(ExpressionNode e)
    {
        this.line = Compiler.lineno;
        expression = e;
        this.type = expression.CheckType();
    }
    public override CompilerType CheckType()
    {
        CompilerType typeToCheck = expression.CheckType();
        if (typeToCheck != CompilerType.Int_Type && typeToCheck != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in bitwise negation at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(typeToCheck)}.");
        }

        return this.type;   
    }

    public override string GenCode()
    {

        if (this.type == CompilerType.Int_Type)
        {
            string curIdent = expression.GenCode();
            string firstSubBuf = Compiler.NewValueIdent();
            string secondSubBuf = Compiler.NewValueIdent();
            Compiler.EmitCode($"{firstSubBuf} = sub i32 {curIdent}, {curIdent}");
            Compiler.EmitCode($"{secondSubBuf} = sub i32 {firstSubBuf}, {curIdent}");
            return secondSubBuf;
        }
        else // wyrazenie typu double
        {
            string curIdent = expression.GenCode();
            string firstSubBuf = Compiler.NewValueIdent();
            string secondSubBuf = Compiler.NewValueIdent();
            Compiler.EmitCode($"{firstSubBuf} = fsub double {curIdent}, {curIdent}");
            Compiler.EmitCode($"{secondSubBuf} = fsub double {firstSubBuf}, {curIdent}");
            return secondSubBuf;
        }
    }
}

// BITWISE_EXPR
public class BitwiseOrNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public BitwiseOrNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;
        this.type = CompilerType.Int_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if(leftType != CompilerType.Int_Type || rightType != CompilerType.Int_Type)
        {
            Compiler.addNewError($"Wrong argument in bitwise or at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        leftExpression.CheckType();
        rightExpression.CheckType();

        string val1 = leftExpression.GenCode();
        string val2 = rightExpression.GenCode();
        string curTempIdent = Compiler.NewValueIdent();
        Compiler.EmitCode($"{curTempIdent} = or i32 {val1}, {val2}");
        return curTempIdent;
    }
}

public class BitwiseAndNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public BitwiseAndNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type || rightType != CompilerType.Int_Type)
        {
            Compiler.addNewError($"Wrong argument in bitwise and at line: {this.line}. Expected int - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        leftExpression.CheckType();
        rightExpression.CheckType();

        string val1 = leftExpression.GenCode();
        string val2 = rightExpression.GenCode();
        string curTempIdent = Compiler.NewValueIdent();
        Compiler.EmitCode($"{curTempIdent} = and i32 {val1}, {val2}");
        return curTempIdent;
    }
}

// MULTIPLICATIVE_EXPR
public class MultipliesNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public MultipliesNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        CompilerType leftType = lE.CheckType();
        CompilerType rightType = rE.CheckType();

        if(leftType == CompilerType.Int_Type && rightType == CompilerType.Int_Type)
        {
            this.type = CompilerType.Int_Type;
        }else
        {
            this.type = CompilerType.Double_type;
        }

    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in multiplies at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in multiplies at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }
        else if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }


        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "mul i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }
                
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fmul double", leftVal, rightVal);    
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class DivisionNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public DivisionNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        CompilerType leftType = lE.CheckType();
        CompilerType rightType = rE.CheckType();

        if (leftType == CompilerType.Int_Type && rightType == CompilerType.Int_Type)
        {
            this.type = CompilerType.Int_Type;
        }
        else
        {
            this.type = CompilerType.Double_type;
        }
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in division at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in division at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }
        else if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "sdiv i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fdiv double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

// ADDITIVE_EXPR
public class PlusNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public PlusNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        CompilerType leftType = lE.CheckType();
        CompilerType rightType = rE.CheckType();

        if (leftType == CompilerType.Int_Type && rightType == CompilerType.Int_Type)
        {
            this.type = CompilerType.Int_Type;
        }
        else
        {
            this.type = CompilerType.Double_type;
        }
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in add at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in add at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        CompilerType relationType;
        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }
        else if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        string tempVal = Compiler.NewValueIdent();
        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "add i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fadd double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class MinusNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public MinusNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        CompilerType leftType = lE.CheckType();
        CompilerType rightType = rE.CheckType();

        if (leftType == CompilerType.Int_Type && rightType == CompilerType.Int_Type)
        {
            this.type = CompilerType.Int_Type;
        }
        else
        {
            this.type = CompilerType.Double_type;
        }
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in subtract at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in subtract at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempIdent = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }
        else if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }
        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempIdent, "sub i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempIdent, "fsub double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempIdent;
    }
}

// RELATIONAL_EXPR
public class EqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public EqualNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if(leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            return this.type;
        }else
        {
            if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
            {
                Compiler.addNewError($"Wrong argument in equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
            }

            if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
            {
                Compiler.addNewError($"Wrong argument in equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
            }
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempIdent = Compiler.NewValueIdent();

        CompilerType relationType;
        if(leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }else if(leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }else
        {
            relationType = CompilerType.Int_Type;
        }
            
        
        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempIdent, "icmp eq i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if(leftType == CompilerType.Int_Type)
                {
                    string convertedVal = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedVal} = sitofp i32 {leftVal} to double");
                    leftVal = convertedVal;
                }

                if(rightType == CompilerType.Int_Type)
                {
                    string convertedVal = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedVal} = sitofp i32 {rightVal} to double");
                    rightVal = convertedVal;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempIdent, "fcmp oeq double", leftVal, rightVal);
                break;
            case CompilerType.Bool_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempIdent, "icmp eq i1", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempIdent;
    }
}

public class UnequalNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public UnequalNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;

    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            return this.type;
        }
        else
        {
            if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
            {
                Compiler.addNewError($"Wrong argument in equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
            }

            if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
            {
                Compiler.addNewError($"Wrong argument in equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
            }
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Bool_Type && rightType == CompilerType.Bool_Type)
        {
            relationType = CompilerType.Bool_Type;
        }
        else if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }


        
        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp ne i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fcmp one double", leftVal, rightVal);
                break;
            case CompilerType.Bool_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp ne i1", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class GreaterNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public GreaterNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in greater at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in greater at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp sgt i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fcmp ogt double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class GreaterOrEqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public GreaterOrEqualNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in greater or equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in greater or equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp sge i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fcmp oge double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class LessNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LessNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in less at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in less at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp slt i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fcmp olt double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

public class LessOrEqualNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;

    public LessOrEqualNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Int_Type && leftType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in less or equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        if (rightType != CompilerType.Int_Type && rightType != CompilerType.Double_type)
        {
            Compiler.addNewError($"Wrong argument in less or equal at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        string leftVal = leftExpression.GenCode();
        string rightVal = rightExpression.GenCode();

        string tempVal = Compiler.NewValueIdent();

        CompilerType relationType;
        if (leftType == CompilerType.Double_type || rightType == CompilerType.Double_type)
        {
            relationType = CompilerType.Double_type;
        }
        else
        {
            relationType = CompilerType.Int_Type;
        }

        switch (relationType)
        {
            case CompilerType.Int_Type:
                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "icmp sle i32", leftVal, rightVal);
                break;
            case CompilerType.Double_type:
                if (leftType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {leftVal} to double");
                    leftVal = convertedIdent;
                }

                if (rightType == CompilerType.Int_Type)
                {
                    string convertedIdent = Compiler.NewValueIdent();
                    Compiler.EmitCode($"{convertedIdent} = sitofp i32 {rightVal} to double");
                    rightVal = convertedIdent;
                }

                Compiler.EmitCode("{0} = {1} {2}, {3}", tempVal, "fcmp ole double", leftVal, rightVal);
                break;
            default:
                break;
        }

        return tempVal;
    }
}

// LOGICAL_EXPR
public class LogicalAndNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;
    string logicalOpPtr;

    public LogicalAndNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;

        logicalOpPtr = Compiler.newLogicalPtrIdent();
        Compiler.LogicalOperationsIdents.Add(logicalOpPtr);
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Bool_Type || rightType != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in logical and at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        leftExpression.CheckType();
        rightExpression.CheckType();

        string outputVal = Compiler.NewValueIdent();
        string outputIdent = Compiler.newPtrIdent();

        string temp1 = Compiler.NewValueIdent();
        string temp2 = Compiler.NewValueIdent();

        int curCondIndex = Compiler.compilerCondIndex++;
        string outputIsFalseLabel = "outputIsFalseLabel" + curCondIndex;
        string checkRExp = "checkRExp" + curCondIndex;
        string endComparison = "endComparison" + curCondIndex;
        string leftValBool = Compiler.NewValueIdent();


        string leftVal = leftExpression.GenCode();
        Compiler.EmitCode($"{leftValBool} = icmp eq i1 1, {leftVal}");
        Compiler.EmitCode($"br i1 {leftValBool}, label %{checkRExp}, label %{outputIsFalseLabel}");
        Compiler.EmitCode($"{checkRExp}:");
        string rightVal = rightExpression.GenCode();
        Compiler.EmitCode($"{temp1} = and i1 {leftVal}, {rightVal}");
        Compiler.EmitCode($"store i1 {temp1}, i1* {logicalOpPtr}");
        Compiler.EmitCode($"br label %{endComparison}");
        Compiler.EmitCode($"{outputIsFalseLabel}:");
        Compiler.EmitCode($"{temp2} = icmp eq i1 1, {leftVal}");
        Compiler.EmitCode($"store i1 {temp2}, i1* {logicalOpPtr}");
        Compiler.EmitCode($"br label %{endComparison}");
        Compiler.EmitCode($"{endComparison}:");
        Compiler.EmitCode($"{outputVal} = load i1, i1* {logicalOpPtr}");

        return outputVal;
    }
}

public class LogicalOrNode : ExpressionNode
{
    ExpressionNode leftExpression;
    ExpressionNode rightExpression;
    string logicalOpPtr;

    public LogicalOrNode(ExpressionNode lE, ExpressionNode rE)
    {
        this.line = Compiler.lineno;
        leftExpression = lE;
        rightExpression = rE;

        this.type = CompilerType.Bool_Type;

        logicalOpPtr = Compiler.newLogicalPtrIdent();
        Compiler.LogicalOperationsIdents.Add(logicalOpPtr);
    }

    public override CompilerType CheckType()
    {
        CompilerType leftType = leftExpression.CheckType();
        CompilerType rightType = rightExpression.CheckType();

        if (leftType != CompilerType.Bool_Type || rightType != CompilerType.Bool_Type)
        {
            Compiler.addNewError($"Wrong argument in or and at line: {this.line}. Expected int or double - got {Compiler.compilerTypeToString(leftType)} and {Compiler.compilerTypeToString(rightType)}.");
        }

        return this.type;
    }

    public override string GenCode()
    {
        leftExpression.CheckType();
        rightExpression.CheckType();

        string outputVal = Compiler.NewValueIdent();
        string outputIdent = Compiler.newPtrIdent();

        string temp1 = Compiler.NewValueIdent();
        string temp2 = Compiler.NewValueIdent();

        int curCondIndex = Compiler.compilerCondIndex++;
        string outputIsTrueLabel = "outputIsFalseLabel" + curCondIndex;
        string checkRExp = "checkRExp" + curCondIndex;
        string endComparison = "endComparison" + curCondIndex;
        string leftValBool = Compiler.NewValueIdent();

        string leftVal = leftExpression.GenCode();
        Compiler.EmitCode($"{leftValBool} = icmp eq i1 1, {leftVal}");
        Compiler.EmitCode($"br i1 {leftValBool}, label %{outputIsTrueLabel}, label %{checkRExp}");
        Compiler.EmitCode($"{checkRExp}:");
        string rightVal = rightExpression.GenCode();
        Compiler.EmitCode($"{temp1} = or i1 {leftVal}, {rightVal}");
        Compiler.EmitCode($"store i1 {temp1}, i1* {logicalOpPtr}");
        Compiler.EmitCode($"br label %{endComparison}");
        Compiler.EmitCode($"{outputIsTrueLabel}:");
        Compiler.EmitCode($"{temp2} = icmp eq i1 1, {leftVal}");
        Compiler.EmitCode($"store i1 {temp2}, i1* {logicalOpPtr}");
        Compiler.EmitCode($"br label %{endComparison}");
        Compiler.EmitCode($"{endComparison}:");
        Compiler.EmitCode($"{outputVal} = load i1, i1* {logicalOpPtr}");
        return outputVal;
    }
}
class CompilerException : ApplicationException
{
    public CompilerException(string msg) : base(msg) { ++Compiler.errors; }
}

