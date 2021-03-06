This document defines many guidelines that should be adhered to when developing
against Banshee. These guidelines will make the codebase more readable,
extensible, and portable.

Reporting Issues
================

[GNOME's bugzilla](https://bugzilla.gnome.org/buglist.cgi?quicksearch=product%3Abanshee)
is the official issue tracker and contributors are encouraged to file bugs and
feature requests there in addition to this repository.

Commit Guidelines
=================

Every change to source code must have a commit message associated to it. The
formatting details of this message are described here:

  http://live.gnome.org/Git/CommitMessages

Please review these guidelines separate to this document, and take in account
these policies on top:

  1. Unlike many other open-source projects, we don't use the git tagging
     'Signed-off-by' to refer as a licencing statement of a contribution,
     rather we use it as 'Reviewed by this maintainer' metadata.

  2. As a consequence of the above, the choice of licencing attribution
     is implicit: by contributing a patch to the Banshee project you are
     automatically accepting to follow the same licence as the project
     itself (see our COPYING file) unless stated on the contrary.

  3. When a contributed patch contains new files, please include licence
     headers in each of them, in the same way the rest of the files of
     the projects already have.

  4. It's recommended that you add yourself (or your company) to the list
     of the copyright owners of a file if you're modifying a considerable
     proportion of it.

  5. Merge commits are prohibited and all merges should fast-forward.


C# Coding Style Guidelines
==========================

These guidelines should be followed when writing code in Banshee. For the most
part they are similar to the Mono syntax guidelines [[1]]. All public API must
adhere to the .NET Framework Design Guidelines. [[2]]

Patches and additions to the code base will be checked for adherence to these
guidelines. If code is in violation, you will be asked to reformat it.

  1. Private variable/field names should be written like:

      `lower_case_with_under_scores`

  2. Property, event, and method names should be written like:

      `UpperCaseStartingLetter`

  3. A space before method/conditional parenthesis, braces:

```C#
if (condition) {
    CallSomeFunction (args);
}
```

  4. One space before a brace on the same line as a conditional or property:

```C#
while (condition) {
    ...
}
```

  5. Namespace, Class, Method braces on separate lines:

```C#
namespace Foo
{
    public class Bar
    {
        private void Method ()
        {
            if (condition) {
                ...
            }
        }
    }
}
```

  6. The exception to rule 5 is for Properties. The brace in the same line
     with the get/set keyword and the respective getter/setter block all
     inline, provided the block is simple:

```C#
public string Something {
    get { return "yay"; }
}
```

  7. If the property accessor block (get/set) is more than one line, use the
     alternative syntax:

```C#
public string Something {
    set {
        DoSomething ();
        something = value;
    }
}
```

  8. There is a space between generic parameters:

      `Dictionary<K, V>` not `Dictionary<K,V>`

  9. Use 4 space characters for indentation, NOT tabs (except in Makefiles)

  10. Try to observe a 120 character wrap margin. If your lines are over 120
      characters, break and indent them logically.

  11. One space at both sides of all type of operators (assignment,
      equality, mathematical, event-subscription, ...):

```C#
var compare = (a + b * c) != (d - e * f);
```

  12. Please write `private` accessors even if the language defaults
      to it in the context you're in.

  13. For tests, use the `Assert.That(actual, Is.EqualTo(expected));` syntax,
      which is more readable, and prevents you from misplacing the expected
      parameter in the place of the actual parameter (a mistake that is very
      usual when using the older `Assert.AreEqual(,)` syntax).

  14. Parameter names should be written like:

      `camelCaseWord`

  15. Simple Branches:

```C#
if (condition) {
    ...
} else if (condition) {
    ...
} else {
    ...
}
```

  16. Switches:

```C#
switch (value) {
    case 1:
        ...
        break;
    default:
        ...
        break;
}
```

  17. Member order should almost always be:

    - fields;
    - constructors;
    - properties;
    - methods.

  18. Static members should almost always precede instance members.


.NET API Naming Guidelines
==========================

  1. Member names should be descriptive and it is best to avoid abbreviations
     and acronyms

  2. If an abbreviation or acronym is used, it should be in the form of an
     accepted name that is generally well known

  3. If an acronym is one-two characters long, it should be all caps

      `Banshee.IO` and not `Banshee.Io`

  4. If an acronym is three or more characters long, only its first letter
     should be capitalized

      - `Banshee.Cdrom`
      - `Banshee.Playlists.Formats.Pls`
      - `Banshee.Playlists.Formats.M3u`

  5. Prefix interfaces with 'I'

      - `IPlaylist`
      - `IImportable`


Implementation Guidelines
=========================

  1. Use generics and generic collections when possible in place of
     1.0 features. New code in Banshee should leverage 2.0 features
     as much as possible, and old code should be updated as development
     occurs in a given area.

      Use `List<T>` instead of `ArrayList`, `Dictionary<K, V>` instead of `Hashtable`

  2. In *most* cases `Banshee.IO` should be used (and possibly extended) when
     IO must be performed. Do *NOT* hard-code `System.IO`, `Mono.Unix`, or
     `Gnome.Vfs` IO into top-level APIs.

  3. When a platform-specific task needs to be performed, a top-level,
     generic API must be designed first and then the platform implementation
     of the API can be added. See `Banshee.Configuration` for ideas.

  4. Do not hard code path separators. Use `Path.DirectorySeparatorChar` instead
     as it is portable to other platforms.

  5. Try not to perform many string concatenations. Use a `StringBuilder` if
     necessary

  6. Avoid calls to `Assembly.GetTypes` as memory resulting from these calls
     will not be GCed.


Organization Guidelines
=======================

  1. Organize code into logical namespaces:

      - `Banshee.Cdrom`
      - `Banshee.Cdrom.Gui`
      - `Banshee.Cdrom.Nautilus`

  2. Try to keep GUI as separate as possible from "real work" and keep
     the namespace separate as well, if possible and applicable. For instance,
     Many different CD-ROM backends could be written for different
     platforms, but the same GUI should be used. Don't put GUI code in
     the platform implementation:

      - `Banshee.Cdrom`
      - `Banshee.Cdrom.Gui`
      - `Banshee.Cdrom.Nautilus`

  3. Banshee's sources are layed out in the following way in the build:

```
src/<high-level-group>/<assembly-name>/<namespace>/<class-or-interface>.cs
```

  4. Small member definitions (delegates, argument classes, enums) can go
     inside the same file containing the primary class, but classes should
     generally be in separate files. Use logical grouping with files.


[1]: http://www.mono-project.com/Coding_Guidelines
[2]: http://msdn2.microsoft.com/en-us/library/ms229042.aspx

