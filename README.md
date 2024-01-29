# Dynamic Proxy Generator using .NET Dynamic Assembly

This project is about creating type(class, property, method) on the fly (runtime).

## Reference

- .NET Dynamic Assembly
- System.Reflection.Emit and System.Reflection.Emit

# (Important) Evaluation Stack

If you are trying to understand IL(Intermedia Language) Generater, You have to knwo evaluation stack concepts.

The evaluation stack is used to keep track of intermediate stages and precedence of operations when evaluating a formula expression. The expression is evaluated from beginning to end, and
**operands are pushed onto the stack as they are encountered.**

the required number of operands is popped from the stack and the result of the operation is pushed back onto the stack.

- "Evaluation Stack" in MS documents (https://learn.microsoft.com/en-us/openspecs/sharepoint_protocols/ms-vgsff/f7048678-3f2d-445f-ae70-6da7e34fe640)

> Formual expression
> A formula expression is a sequence of values and functions that, when evaluated, produce a new value. formula expressions are stored as strings using Reverse-Polish notation.
> Reverse-Polist Notation(RPN): https://en.wikipedia.org/wiki/Reverse_Polish_notation
