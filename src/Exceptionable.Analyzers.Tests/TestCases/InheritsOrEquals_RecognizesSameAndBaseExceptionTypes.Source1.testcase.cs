// Verifies Inherits Or Equals Recognizes Same And Base Exception Types.

using System;
class CustomException : Exception { }
class DerivedException : CustomException { }
class OtherException : Exception { }

