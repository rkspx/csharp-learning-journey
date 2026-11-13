# Day 1 Note

Today I built a C# console program that reads a JSON alert, validates its required fields, and prints a summary.

I used three classes: `Alert`, `Agent`, and `Rule`. The `Alert` class contains properties whose types are `Agent` and `Rule`, matching the nested objects in the JSON.

I can write a local function inside a top-level C# program using this structure:

```csharp
static ReturnType FunctionName(ParameterType parameter)
{
    // Function body
}
```

My `ReadAlert` function takes a file path and returns an `Alert`. C# convention uses PascalCase for function and method names. C# also supports methods declared inside classes.

To read a file into a string, I use the static method `File.ReadAllText(filename)`. Reading can fail with an `IOException`, such as when a file is missing, or an `UnauthorizedAccessException` when access is denied. Relative paths are resolved from the program’s current working directory.

To convert JSON text into an object, I use:

```csharp
Alert? alert = JsonSerializer.Deserialize<Alert>(json);
```

`<Alert>` tells the method which type to deserialize into. Deserialization can throw a `JsonException` if the syntax is invalid or a value is incompatible with the model. A JSON document containing only `null` produces a null result.

I can reject that null result immediately:

```csharp
Alert alert = JsonSerializer.Deserialize<Alert>(json)
    ?? throw new InvalidDataException("The file must contain an alert object.");
```

The `??` operator uses the expression on its right when the left side is null. Here, it throws an exception. If execution continues, `alert` contains an object.

Valid JSON can still be invalid input for my application. For example, an alert might deserialize successfully but have no ID. I validate those requirements separately.

`int?` can hold an integer or null. A plain `int` cannot hold null. Using `int?` for `Level` lets me distinguish a missing value from a supplied zero. My validation accepts `0` and rejects null.

I use `[JsonPropertyName("id")]` to map a C# property such as `Id` to a JSON name. The mapping applies during both serialization and deserialization. Unknown JSON properties are ignored by default. That lets my reader accept additional data, though it can also hide misspelled fields.

To signal a failure, I can throw an exception:

```csharp
throw new InvalidDataException("Rule level is required.");
```

`Exception` is the general base type. Choosing a more specific type lets the calling code handle that category of failure separately.

I put code that may throw inside a `try` block, then handle exceptions with `catch` blocks. Valid forms include:

```csharp
catch (JsonException e)
catch (JsonException)
catch
```

Each form needs a block body. I include a variable such as `e` when I want to inspect the exception. Multiple handlers are checked from top to bottom, so more specific handlers belong before broader ones. If I include `catch (Exception e)`, it goes last.

When a function throws, it stops normal execution and the exception travels to a matching handler. The function doesn’t return its expected result. A stack trace shows where the exception originated and the calls that led there; a listed call site isn’t necessarily incorrect.

Printing `e` includes exception details and a stack trace. Printing `e.Message` gives just the message. For a readable console error, I used:

```csharp
Console.Error.WriteLine($"Error: {e.Message}");
```

Returning `0` from the program’s entry point signals success. A nonzero exit code signals failure; I used `1`. In the terminal, `echo $?` prints the previous command’s exit code, so I run it immediately after the command I want to check.

The `!` in `alert.Agent!` suppresses a compiler nullable warning. It doesn’t validate the object or prevent a null-reference error. I rely on my earlier validation before using it.

I also used a conditional expression to provide a fallback name:

```csharp
string name = string.IsNullOrWhiteSpace(alert.Agent!.Name)
    ? "unknown"
    : alert.Agent.Name;
```

The structure is `condition ? valueWhenTrue : valueWhenFalse`. Missing, empty, or whitespace-only names become `"unknown"`. The agent object itself is still required.

Finally, string interpolation lets me insert values into output using `$"..."` and expressions inside braces, such as `$"name={name}"`.