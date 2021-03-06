using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentNHibernate;
using Trinity.Core.Configuration;
using Trinity.Encore.Game.Database;
using Trinity.Persistence;

namespace Trinity.Encore.AccountService.Database.Implementation
{
    public sealed class AccountDatabaseContext : GameDatabaseContext
    {
        [ConfigurationVariable("SqlType", DatabaseType.MySql, Static = true)]
        public static DatabaseType DatabaseType { get; set; }

        [ConfigurationVariable("SqlConnectionString", "Server=127.0.0.1;Database=Encore.Account;User ID=Encore;Password=Encore",
            Static = true)]
        public static string ConnectionString { get; set; }

        [SuppressMessage("Microsoft.Contracts", "Requires", Justification = "Configuration checked on startup.")]
        public AccountDatabaseContext()
            : base(DatabaseType, ConnectionString)
        {
        }

        protected override IEnumerable<IMappingProvider> CreateMappings()
        {
            yield return new AccountMapping();
            yield return new AccountBanMapping();
            yield return new IPBanMapping();
            yield return new IPRangeBanMapping();
        }
    }
}
