using System.Runtime.CompilerServices;

// Castle DynamicProxy (used by NSubstitute) must be able to see the internal test
// doubles it closes generic product interfaces over.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
