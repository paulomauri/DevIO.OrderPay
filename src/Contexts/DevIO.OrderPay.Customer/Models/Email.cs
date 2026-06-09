using System.Text.RegularExpressions;

namespace DevIO.OrderPay.Customer.Models;

public partial class Email
{
    private static readonly Regex _emailRegex = EmailRegex();

    public Email(string value)
    {
        // validação
        if (!_emailRegex.IsMatch(value))
        {
            throw new ArgumentException("E-mail inválido.");
        }

        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "pt-BR")]
    private static partial Regex EmailRegex();
}
