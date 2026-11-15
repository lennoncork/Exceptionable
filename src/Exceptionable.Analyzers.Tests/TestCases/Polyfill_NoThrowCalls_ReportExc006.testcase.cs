// Certain calls to no-throw polyfill members.
// Expected behavior: each framework or base try/catch is EXC006. Safe does not report EXC003.
// Token.ToString is a source override with no [DoesNotThrow], so its call is not EXC006 and the method is EXC009.
using System;
using System.Collections.Generic;
using Exceptionable;

struct Token
{
    public override string ToString() => "token";
}

class Calls
{
    [DoesNotThrow]
    void Safe(string value) { value.ToString(); }

    void StringToString(string value) { try { value.ToString(); } catch (Exception) { } }
    void StringEquals(string value, string other) { try { value.Equals(other); } catch (Exception) { } }
    void Int32Hash(int value) { try { value.GetHashCode(); } catch (Exception) { } }
    void Int32Equals(int value) { try { value.Equals(1); } catch (Exception) { } }
    void ReferenceEquals(object left, object right) { try { object.ReferenceEquals(left, right); } catch (Exception) { } }
    void StringCompare(string value, string other) { try { value.CompareTo(other); } catch (Exception) { } }
    void ComparerEquals(IEqualityComparer<string> comparer, string left, string right) { try { comparer.Equals(left, right); } catch (Exception) { } }
    void GenericCompare(IComparable<int> value) { try { value.CompareTo(1); } catch (Exception) { } }
    void BaseToString() { try { base.ToString(); } catch (Exception) { } }
    void StructToString(Token token) { try { token.ToString(); } catch (Exception) { } }
}
