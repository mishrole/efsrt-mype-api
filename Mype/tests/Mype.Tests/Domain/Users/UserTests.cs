using FluentAssertions;
using Mype.Domain.Users;
using System;

namespace Mype.Tests.Domain.Users
{
    public class UserTests
    {
        private const string Email = "user@example.com";
        private const string NormalizedEmail = "USER@EXAMPLE.COM";
        private const string PasswordHash = "password-hash";
        private const string DisplayName = "Test User";

        private static readonly DateTimeOffset CreatedAt = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Create_Should_Initialize_User()
        {
            var user = CreateUser();

            user.Id.Should().NotBeEmpty();
            user.Email.Should().Be(Email);
            user.NormalizedEmail.Should().Be(NormalizedEmail);
            user.PasswordHash.Should().Be(PasswordHash);
            user.DisplayName.Should().Be(DisplayName);
            user.EmailVerified.Should().BeFalse();
            user.Status.Should().Be(UserStatus.Active);
            user.DeactivatedAt.Should().BeNull();
            user.CreatedAt.Should().Be(CreatedAt);
            user.UpdatedAt.Should().Be(CreatedAt);
            user.IsActive().Should().BeTrue();
        }

        [Fact]
        public void Deactivate_Should_Set_User_As_Inactive()
        {
            var deactivatedAt = CreatedAt.AddHours(1);
            var user = CreateUser();

            user.Deactivate(deactivatedAt);

            user.Status.Should().Be(UserStatus.Inactive);
            user.DeactivatedAt.Should().Be(deactivatedAt);
            user.UpdatedAt.Should().Be(deactivatedAt);
            user.IsActive().Should().BeFalse();
        }

        [Fact]
        public void Reactivate_Should_Set_User_As_Active()
        {
            var reactivatedAt = CreatedAt.AddHours(2);
            var user = CreateUser();

            user.Deactivate(CreatedAt.AddHours(1));
            user.Reactivate(reactivatedAt);

            user.Status.Should().Be(UserStatus.Active);
            user.DeactivatedAt.Should().BeNull();
            user.UpdatedAt.Should().Be(reactivatedAt);
            user.IsActive().Should().BeTrue();
        }

        private static User CreateUser()
        {
            return User.Create(
                Email,
                NormalizedEmail,
                PasswordHash,
                DisplayName,
                CreatedAt
            );
        }
    }
}
