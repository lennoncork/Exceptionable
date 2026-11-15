// Overloads and virtual calls whose contract is not no-throw.
// Expected behavior: no EXC006. Format ToString, comparison Equals, non-generic
// CompareTo, static Object.Equals, GetHashCode(T), and open virtual calls stay unchecked.
using System;
using System.Collections;
using System.Collections.Generic;

class Widget
{
    public override string ToString() => "widget";
}

class Calls
{
    void Format(int value) { try { value.ToString("D"); } catch (Exception) { } }
    void Comparison(string value, string other, StringComparison comparison) { try { value.Equals(other, comparison); } catch (Exception) { } }
    void NonGenericCompare(string value, object other) { try { value.CompareTo(other); } catch (Exception) { } }
    void StaticEquals(object left, object right) { try { object.Equals(left, right); } catch (Exception) { } }
    void HashCode(IEqualityComparer<string> comparer, string value) { try { comparer.GetHashCode(value); } catch (Exception) { } }
    void NonGenericEquals(IEqualityComparer comparer, object left, object right) { try { comparer.Equals(left, right); } catch (Exception) { } }
    void OpenVirtual(object value) { try { value.ToString(); } catch (Exception) { } }
    void Unsealed(Widget value) { try { value.ToString(); } catch (Exception) { } }
}
