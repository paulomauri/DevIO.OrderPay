using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DevIO.OrderPay.Customer.Models;

public static class UfStringConverter
{
    public static State? From(string? uf)
    {
        if (uf is null) return null;
        return Enum
            .TryParse<State>(uf, out var parsedUf) ? parsedUf : null;
    }

    public static string ToStringValue(this State? uf)
    {
        return uf switch
        {
            State.AC => "AC",
            State.AL => "AL",
            State.AP => "AP",
            State.AM => "AM",
            State.BA => "BA",
            State.CE => "CE",
            State.DF => "DF",
            State.ES => "ES",
            State.GO => "GO",
            State.MA => "MA",
            State.MT => "MT",
            State.MS => "MS",
            State.MG => "MG",
            State.PA => "PA",
            State.PB => "PB",
            State.PE => "PE",
            State.PR => "PR",
            State.PI => "PI",
            State.RJ => "RJ",
            State.RN => "RN",
            State.RS => "RS",
            State.RO => "RO",
            State.RR => "RR",
            State.SC => "SC",
            State.SP => "SP",
            State.SE => "SE",
            State.TO => "TO",
            _ => uf?.ToString().ToUpperInvariant() ?? ""
        };
    }
}



public enum State
{
    [Description("AC")]
    AC,
    [Description("AL")]
    AL,
    [Description("AP")]
    AP,
    [Description("AM")]
    AM,
    [Description("BA")]
    BA,
    [Description("CE")]
    CE,
    [Description("DF")]
    DF,
    [Description("ES")]
    ES,
    [Description("GO")]
    GO,
    [Description("MA")]
    MA,
    [Description("MT")]
    MT,
    [Description("MS")]
    MS,
    [Description("MG")]
    MG,
    [Description("PA")]
    PA,
    [Description("PB")]
    PB,
    [Description("PR")]
    PR,
    [Description("PE")]
    PE,
    [Description("PI")]
    PI,
    [Description("RJ")]
    RJ,
    [Description("RN")]
    RN,
    [Description("RS")]
    RS,
    [Description("RO")]
    RO,
    [Description("RR")]
    RR,
    [Description("SC")]
    SC,
    [Description("SP")]
    SP,
    [Description("SE")]
    SE,
    [Description("TO")]
    TO
}