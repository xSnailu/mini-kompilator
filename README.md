
# MiniCompiler
Compiler for my own programming language.
# Language specification:

## Language elements:
Language contains following terminals:
- keywords: **program if else while read write return int double bool true false**
- operators and special symbols: **= || && | & == != > >= < <= + - * / ! ~ ( ) { } ;**
- identifiers and numbers

**Identifier** is string containing letters and digits, starting with letter. Both upper and lowercase letters are allowed and are distinguishable.
**Floating point number** is string of digits meaning integer part of a number, dot and fractional part. Starting zero is allowed only when it is the only digit of integer part.
**Integer number** is string of digits.

Additionally, all white characters (**spaces, tabulators, new lines**) are ignored, but separate elements.

Language allow **comments** starting with **//**. All characters in line after comment symbol are ignored.
**String** is any text in **" "** and can be used only in write instruction.

## Program
Program starts with **program** keyword followed by set of declarations and instructions in brackets.

## Declarations
Single declaration contains type, identifier and semicolon. Allowed types are: **int**, **double** and **bool**. All variables are initialized by default values (zero or false).

Use of undeclared variable causes compilation error with "undeclared variable" message.
All identifiers must be unique.

## Instructions
Language contains 7 instructions:
- block statement **{ code }**
- expression
- conditional statement
- while loop
- read
- write
- return


## Example programs

1)
```
program {
	write (5 + 5) * 2.5 - 18 == (int) 7.35;
	write 1 / 2 == (double) 1 / 2;
	write (double) 64 / 2 / 2 / 2 / 2 / 2 / 2;
	write (4 - 2 - 1) * (5 - 3 - 1) * ((-2 + 3) + 1 - (int)!(true == false)); 
	write 2 + 3 * 3 + 5 + 10 / 2 - 6;
	write 1.5 * 1.5 * 1.5 * 1.5 * 1.5 * 1.5 * 1.5;
}
```

Output:
```
True
False
1.000000
1
15
17.085938
```
2)
```
program {
	bool b;
	bool c;
	int cnt;
	int cntTemp;
	b = c = true;
	cnt = 3;
	while (b || c) {
		cntTemp = cnt;
		while(b == true && c == true && cntTemp > 0){
			write --cntTemp;
			write " ";
			cntTemp = cntTemp - 1;
		}	
		if(cnt == 1)
			b = false;
		if(cnt == 0) {
			write "end";
			return;
		}
		cnt = cnt - 1;
	}
}
```
Output:
```
3 2 1 2 1 1 end
```

3) 
```
program	{
	int i;
	double d;
	bool b;
	i = 5;
	d = 123.456;
	b = true;
	write i;
	write "\n";
	write d;
	write "\n";
	write b;
	write "\n";
}
```

Output:
```
5
123.456000
True
```

4) 
```
program {
	int a;
	int b;
	a = 10;
	b = -21;
	if(a < b)
		write "FAIL\n";
	if(a <b) {
		write "FAIL";
		write "FAIL";
	}
	write "OK\n";
	if(a >b) {
		write "OK";
		write "OK";
	}
	write "OK\n";
	if (a <b) {
		write "FAIL";
		write "FAIL";
	}
	else {
		write "OK";
		write "OK";
	}
	write "OK";
	if (a <b)
		write "FAIL";
	else
		write "OK";
	write "FINISH";
}
```

Output:

```
OK
OKOKOK
OKOKOKOKFINISH
```

5)
```
program {
	int a;
	int b;
	bool c;
	a = 10;
	b = 11;
	c = false;
	if (a==b && (a=20)>0) {}
	if (20 == a)
		write "FAIL";
	c = 15 != 41 || (a = -1) == 5;
	if (a == -1)
		write "FAIL";
	c = 15 != 41 || 21 == 21 || (a = 0) == 2;
	if (a == 0)
		write "FAIL";
	c = 15 == 41 || 21 == 20 || (a = 10) == 5;
	if (a != 10)
		write "FAIL";
	write "OK";
}
```

Output:

```
OK
```

6)
```
program {
	if ( true )
		if ( true )
			write "poo\n";
		else
			write "boo\n";
	if ( true )
		if ( false )
			write "too\n";
		else
			write "doo\n";
	if ( false )
		if ( true )
			write "goo\n";
		else
			write "koo\n";
	if ( false )
		if ( false )
			write "woo\n";
		else
			write "foo\n";
}
```

Output:

```
poo
doo
```

## Running program

To build compiler run following command in main project folder:
```
gplex /out:lexer.cs kompilator.lex
gppg /gplex /out:parser.cs kompilator.y
```

Move file with source code to bin/Debug folder. Run project and type its name from keyboard. After successful compilation, .ll file is generated in bin/Debug folder. You can run it using LLVM lli tool. 
