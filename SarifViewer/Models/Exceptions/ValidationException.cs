using System;

namespace SarifViewer.Models.Exceptions;

public class ValidationException: Exception
{
    public ValidationException(string message): base(message)
    {
    }
}
