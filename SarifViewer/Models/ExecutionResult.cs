namespace SarifViewer.Models;

public record ExecutionResult(bool IsSuccessful, string ErrorMessage = null);