namespace DevIO.OrderPay.Customer.Exceptions;

public class DuplicateCpfException : Exception
{
    public DuplicateCpfException(string cpf)
        : base($"CPF '{cpf}' is already registered.") { }
}
