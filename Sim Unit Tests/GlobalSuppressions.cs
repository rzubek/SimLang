// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Assertion", "NUnit2002:Consider using Assert.That(expr, Is.False) instead of Assert.IsFalse(expr)", Justification = "Readability")]
[assembly: SuppressMessage("Assertion", "NUnit2003:Consider using Assert.That(expr, Is.True) instead of Assert.IsTrue(expr)", Justification = "Readability")]
[assembly: SuppressMessage("Assertion", "NUnit2010:Use EqualConstraint for better assertion messages in case of failure", Justification = "Readability")]
[assembly: SuppressMessage("Assertion", "NUnit2017:Consider using Assert.That(expr, Is.Null) instead of Assert.IsNull(expr)", Justification = "Readability")]
[assembly: SuppressMessage("Assertion", "NUnit2019:Consider using Assert.That(expr, Is.Not.Null) instead of Assert.IsNotNull(expr)", Justification = "Readability")]
