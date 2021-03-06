# Piece Table implementation in C#

## What is the Piece Table?

Piece Table isn't a very common data structure and therefore isn't well documented.

Here are a few helpful article that inspired me:

* [The Piece Table, by Darren Burns](https://darrenburns.net/posts/piece-table/)

* [Text Buffer Reimplementation, a Visual Studio Code Story](https://code.visualstudio.com/blogs/2018/03/23/text-buffer-reimplementation#)

* [Piece table - Wikipedia](https://en.wikipedia.org/wiki/Piece_table)

### TL;DR

from Wikipedia:

> A **piece table** is a data structure typically used to represent a series of edits on a text document. An initial reference (or 'span') to the whole of the original file is created, with subsequent inserts and deletes being created as combinations of one, two, or three references to sections of either the original document or of the spans associated with earlier inserts.
> 
> Typically the text of the original document is held in one immutable block, and the text of each subsequent insert is stored in new immutable blocks. Because even deleted text is still included in the piece table, this makes multi-level or unlimited undo easier to implement with a piece table than with alternative data structures such as a gap buffer.

## Motivation of this repository

I was curious about the implementation details of such a data structure, since I myself work on the text editor of Visual Studio at Microsoft. However, existing implementation available on GitHub, [including the one of Visual Studio itself](https://github.com/microsoft/vs-editor-api/tree/main/src/Editor/Text/Impl/TextModel/StringRebuilder), are either:

1. Complex to read because tightly linked to a product like VS or VS Code, and not generic enough to be reused as is.

2. Incomplete and/or buggy.

3. Not documented / commented.

4. In functional or procedural language (nothing against it but I like oriented object programming).

So I thought `let's have fun and do it myself, hoping to have something reliable, generic and easy to understand`.

## Implementation

If you take 10 minutes to read [The Piece Table, by Darren Burns](https://darrenburns.net/posts/piece-table/) and understand well how the Piece Table is supposed to work, then reading the code in this repository should be relatively easy as it reuses some terms you can find in this blog article.

* `TextDocumentBuffer` hold the buffers that allow to represent and rebuild the text as a `string` after an insertion / deletion in the text document.

* `TextPieceTable` hosts the `piece table` itself and the logic for inserting / deleting a piece. It has a bunch of logic for translating coordinates from the text document to a piece in the buffer. I uses a `LinkedList<Piece>` for representing the list of pieces. 

* `Piece` represents a piece in the piece table. It has the following information:
  
  * Whether this piece is from the original or append buffer.
  
  * A `Span` representing where the piece is in the buffer.

* `Span` represents a range in a buffer as well in the text document with a start position and length.

A unit test project validates the good behavior of the implementation. Hopefully it's covering enough scenarios to make it reliable.

A benchmark project helped me at identifying some flaws in the implementation and improve it.

## Benchmarks

To run the benchmark, simply do in a PowerShell command prompt:

```powershell
> cd Benchmarks
> dotnet run -c Release
```

### Results

```ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000

AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores

.NET SDK=6.0.101

 [Host] : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT



Job=InProcess Toolchain=InProcessEmitToolchain InvocationCount=1 

UnrollFactor=1 
```

| Method                                             | Initial amount of pieces in the piece table  | Mean            | Median          | Allocated    |
| -------------------------------------------------- | -------------------------------------------- | ---------------:| ---------------:| ------------:|
| Insertion near the end of the document             | 1000                                         | 2.126 ??s        | 1.900 ??s        | 960 B        |
| Insertion near the beginning of the document       | 1000                                         | 1.857 ??s        | 1.600 ??s        | 480 B        |
| Insertion in the middle of the document            | 1000                                         | 2.621 ??s        | 2.500 ??s        | 704 B        |
| Insertion near the end of the document             | 10000                                        | 1.397 ??s        | 1.300 ??s        | 816 B        |
| Insertion near the beginning of the document       | 10000                                        | 1.338 ??s        | 1.200 ??s        | 816 B        |
| Insertion in the middle of the document            | 10000                                        | 10.286 ??s       | 10.300 ??s       | 704 B        |
| Insertion near the end of the document             | 100000                                       | 3.839 ??s        | 3.800 ??s        | 816 B        |
| Insertion near the beginning of the document       | 100000                                       | 4.171 ??s        | 3.950 ??s        | 1,104 B      |
| Insertion in the middle of the document            | 100000                                       | 120.888 ??s      | 120.850 ??s      | 704 B        |
| Insertion near the end of the document             | 1000000                                      | 5.581 ??s        | 5.500 ??s        | 816 B        |
| Insertion near the beginning of the document       | 1000000                                      | 6.025 ??s        | 5.900 ??s        | 528 B        |
| Insertion in the middle of the document            | 1000000                                      | 3,145.587 ??s    | 3,106.300 ??s    | 992 B        |
| Delete near the end of the document                | 1000                                         | 1.851 ??s        | 1.800 ??s        | 1,248 B      |
| Delete near the beginning of the document          | 1000                                         | 2.038 ??s        | 1.900 ??s        | 1,248 B      |
| Delete in the middle of the document               | 1000                                         | 3.008 ??s        | 2.800 ??s        | 2,296 B      |
| Delete near the end of the document                | 10000                                        | 1.444 ??s        | 1.300 ??s        | 1,584 B      |
| Delete near the beginning of the document          | 10000                                        | 1.413 ??s        | 1.300 ??s        | 1,248 B      |
| Delete in the middle of the document               | 10000                                        | 11.212 ??s       | 11.200 ??s       | 1,200 B      |
| Delete near the end of the document                | 100000                                       | 5.727 ??s        | 5.700 ??s        | 912 B        |
| Delete near the beginning of the document          | 100000                                       | 6.146 ??s        | 6.100 ??s        | 912 B        |
| Delete in the middle of the document               | 100000                                       | 110.200 ??s      | 109.400 ??s      | 1,200 B      |
| Delete near the end of the document                | 1000000                                      | 8.709 ??s        | 8.450 ??s        | 1,872 B      |
| Delete near the beginning of the document          | 1000000                                      | 8.664 ??s        | 8.300 ??s        | 960 B        |
| Delete in the middle of the document               | 1000000                                      | 2,887.207 ??s    | 2,852.200 ??s    | 624 B        |
| Get some text near the end of the document         | 1000                                         | 3,467.0 ns      | 2,400.0 ns      | 1,080 B      |
| Get some text near the beginning of the document   | 1000                                         | 2,008.1 ns      | 1,800.0 ns      | 1,416 B      |
| Get some text in the middle of the document        | 1000                                         | 3,249.5 ns      | 3,300.0 ns      | 1,408 B      |
| Get a character near the end of the document       | 1000                                         | 595.7 ns        | 600.0 ns        | -            |
| Get a character near the beginning of the document | 1000                                         | 390.9 ns        | 400.0 ns        | -            |
| Get a character in the middle of the document      | 1000                                         | 1,563.5 ns      | 1,600.0 ns      | 672 B        |
| Get all the text in the document                   | 1000                                         | 28,003.2 ns     | 27,500.0 ns     | 61,424 B     |
| Get some text near the end of the document         | 10000                                        | 1,840.4 ns      | 1,700.0 ns      | 1,472 B      |
| Get some text near the beginning of the document   | 10000                                        | 1,944.9 ns      | 1,800.0 ns      | 792 B        |
| Get some text in the middle of the document        | 10000                                        | 12,395.6 ns     | 12,000.0 ns     | 1,408 B      |
| Get a character near the end of the document       | 10000                                        | 344.7 ns        | 300.0 ns        | -            |
| Get a character near the beginning of the document | 10000                                        | 401.0 ns        | 400.0 ns        | 624 B        |
| Get a character in the middle of the document      | 10000                                        | 10,010.0 ns     | 9,900.0 ns      | 672 B        |
| Get all the text in the document                   | 10000                                        | 226,790.3 ns    | 225,700.0 ns    | 739,568 B    |
| Get some text near the end of the document         | 100000                                       | 7,812.1 ns      | 7,700.0 ns      | 840 B        |
| Get some text near the beginning of the document   | 100000                                       | 8,168.5 ns      | 8,100.0 ns      | 840 B        |
| Get some text in the middle of the document        | 100000                                       | 114,848.5 ns    | 112,700.0 ns    | 1,120 B      |
| Get a character near the end of the document       | 100000                                       | 1,102.1 ns      | 1,100.0 ns      | -            |
| Get a character near the beginning of the document | 100000                                       | 1,732.0 ns      | 1,700.0 ns      | -            |
| Get a character in the middle of the document      | 100000                                       | 105,804.0 ns    | 103,400.0 ns    | 960 B        |
| Get all the text in the document                   | 100000                                       | 3,166,211.5 ns  | 3,144,600.0 ns  | 6,209,184 B  |
| Get some text near the end of the document         | 1000000                                      | 11,806.2 ns     | 10,950.0 ns     | 1,128 B      |
| Get some text near the beginning of the document   | 1000000                                      | 11,375.3 ns     | 11,000.0 ns     | 1,080 B      |
| Get some text in the middle of the document        | 1000000                                      | 2,890,640.2 ns  | 2,841,000.0 ns  | 832 B        |
| Get a character near the end of the document       | 1000000                                      | 2,253.3 ns      | 2,100.0 ns      | 576 B        |
| Get a character near the beginning of the document | 1000000                                      | 2,310.5 ns      | 2,300.0 ns      | 336 B        |
| Get a character in the middle of the document      | 1000000                                      | 2,763,792.9 ns  | 2,756,250.0 ns  | 672 B        |
| Get all the text in the document                   | 1000000                                      | 37,957,515.0 ns | 38,045,400.0 ns | 53,602,496 B |

## Analysis

### Inserting or deleting at the beginning and the end of the text document is fast.

That's because we're reading the table of pieces (which is a `LinkedList<Piece>`) sequentially to find where does the given span / text should be inserted / deleted. Based on the location in the text document where we want to do the insertion / deletion, the program decides whether it should navigate forward (from the beginning to the end) or backward (from the end to the beginning) in the table of pieces.

### Inserting or deleting in the middle of the text document is slow.

Since the table of pieces is a `LinkedList`, we have to read it sequentially. When we're trying to access to the middle of the it, reading forward or backward like explained above doesn't help. We have to pay the cost of going through more items to reach the middle.

A potential way to improve it is to transform the piece table into a "piece-tree" that would basically represents the pieces in a hierarchical manner where each node also give us some information on the state of the text document. Pretty much what's explained here: https://code.visualstudio.com/blogs/2018/03/23/text-buffer-reimplementation#_boost-line-lookup-by-using-a-balanced-binary-tree

This approach should allow to perform a binary search.

### If it's slow to go through a `LinkedList`, why not using a `List`, which would allow binary search?

Sure, in C#, reading the list would be faster since we can access to any item through the indexer like `myList[i]` and could perform binary search. However, assuming this data structure is used in the context of a text editor, as a user, I can typing anywhere in the document, which would require to do a `List.Insert` instead of `List.Add`. Unfortunately, `List.Insert` is very slow on a large list and internally does a copy of the list. The impact of it is that inserting and deleting in the piece table would be much slower. Assuming again that this data structure is used in the context of a text editor, this means that the typing responsiveness (how fast does the text editor answers to user type) would likely be impacted.

A good StackOverlow answer about performance difference between LinkedList and List: [c# - When should I use a List vs a LinkedList - Stack Overflow](https://stackoverflow.com/a/29263914)

With all that said, it might worth experimenting with other collection types like  `ImmutableList` for example. But at the end of the day, a true performance improvement, in the context of a text editor, is not to do a piece table, but a "piece tree" using, for example, an AVL Tree or Red-Black Tree.

## Feedback / Contributing

Found an optimization to do, a bug or a scenario not covered by unit tests? Feel free to open an issue or a pull request.
