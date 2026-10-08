using Mype.Application.Common.Interfaces;
using System;

namespace Mype.Application.Common.Normalizers
{
    public class EmailNormalizer : IEmailNormalizer
    {
        public string Normalize(string email)
        {
            ArgumentNullException.ThrowIfNull(email);

            return email
            .Trim()
            .ToUpperInvariant();
        }
    }
}
