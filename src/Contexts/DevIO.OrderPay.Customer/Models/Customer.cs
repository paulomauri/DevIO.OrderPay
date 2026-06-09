using System.Diagnostics.CodeAnalysis;

namespace DevIO.OrderPay.Customer.Models;

public class Customer
{
    private Customer() { }

    [SetsRequiredMembers]
    public Customer(string nome, Email email, string cpf, string mobile)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        Email = email;
        CPF = cpf;
        Celular = mobile;
    }

    public Guid Id { get; set; }
    public string Nome { get; private set; } = string.Empty;
    public required Email Email { get; set; }
    public string CPF { get; private set; } = string.Empty;
    public string? Celular { get; set; } = string.Empty;
    public List<Address> Enderecos { get; set; } = [];

    public void Update(string nome, string email, string cpf, string mobile)
    {
        Nome = nome;
        CPF = cpf;
        Celular = mobile;
        Email = new Email(email);
    }

    public Address AddEndereco(Address endereco)
    {
        Enderecos ??= [];
        Enderecos.Add(endereco);
        return endereco;
    }

    public void RemoveEndereco(Address endereco)
    {
        if (Enderecos.Count > 1)
            Enderecos.Remove(endereco);
    }

    public Address AddEndereco(string cep, string rua, string? numero, string? complemento, string? bairro, string municipio, State? estado)
    {
        var endereco = new Address
        {
            CEP = cep,
            Rua = rua,
            Numero = numero,
            Complemento = complemento,
            Bairro = bairro,
            Municipio = municipio,
            Estado = estado
        };
        return AddEndereco(endereco);
    }
}
