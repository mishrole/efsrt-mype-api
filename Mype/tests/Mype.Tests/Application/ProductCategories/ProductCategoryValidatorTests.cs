using System;
using FluentAssertions;
using Mype.Application.ProductCategories.Commands.CreateProductCategory;
using Mype.Application.ProductCategories.Commands.DeactivateProductCategory;
using Mype.Application.ProductCategories.Commands.ReactivateProductCategory;
using Mype.Application.ProductCategories.Commands.UpdateProductCategory;
using Mype.Application.ProductCategories.Queries.GetProductCategoryDetail;
using Mype.Application.ProductCategories.Queries.ListProductCategories;
using Mype.Domain.ProductCategories.Constraints;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class ProductCategoryValidatorTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid CurrentUserId = Guid.NewGuid();
        private static readonly Guid ProductCategoryId = Guid.NewGuid();

        [Fact]
        public void Create_Should_Accept_Missing_Color_For_Quick_Creation()
        {
            var command = new CreateProductCategoryCommand
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Name = "Bebidas",
                Color = null,
            };
            new CreateProductCategoryCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("#3F6FC4")]
        [InlineData("#ffffff")]
        public void Create_Should_Accept_Valid_Hexadecimal_Color(string color)
        {
            var command = new CreateProductCategoryCommand
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Name = "Bebidas",
                Color = color,
            };
            new CreateProductCategoryCommandValidator().Validate(command).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("3F6FC4")]
        [InlineData("#FFF")]
        [InlineData("#GGGGGG")]
        public void Create_Should_Reject_Invalid_Color(string color)
        {
            var command = new CreateProductCategoryCommand
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Name = "Bebidas",
                Color = color,
            };
            new CreateProductCategoryCommandValidator()
                .Validate(command)
                .IsValid.Should()
                .BeFalse();
        }

        [Fact]
        public void Create_Should_Reject_Name_That_Exceeds_Maximum_Length()
        {
            var command = new CreateProductCategoryCommand
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Name = new string('a', ProductCategoryConstraints.NameMaxLength + 1),
            };
            new CreateProductCategoryCommandValidator()
                .Validate(command)
                .IsValid.Should()
                .BeFalse();
        }

        [Fact]
        public void Update_Should_Require_Identifiers_Color_And_Version()
        {
            var command = new UpdateProductCategoryCommand { Name = "Bebidas" };
            var validationResult = new UpdateProductCategoryCommandValidator().Validate(command);
            validationResult.IsValid.Should().BeFalse();
            validationResult
                .Errors.Should()
                .Contain(validationFailure =>
                    validationFailure.PropertyName
                    == nameof(UpdateProductCategoryCommand.ProductCategoryId)
                );
            validationResult
                .Errors.Should()
                .Contain(validationFailure =>
                    validationFailure.PropertyName == nameof(UpdateProductCategoryCommand.Color)
                );
            validationResult
                .Errors.Should()
                .Contain(validationFailure =>
                    validationFailure.PropertyName == nameof(UpdateProductCategoryCommand.Version)
                );
        }

        [Fact]
        public void Status_Validators_Should_Accept_Complete_Commands()
        {
            var deactivateCommand = new DeactivateProductCategoryCommand
            {
                BusinessId = BusinessId,
                ProductCategoryId = ProductCategoryId,
                CurrentUserId = CurrentUserId,
                Version = 1,
            };
            var reactivateCommand = new ReactivateProductCategoryCommand
            {
                BusinessId = BusinessId,
                ProductCategoryId = ProductCategoryId,
                CurrentUserId = CurrentUserId,
                Version = 1,
            };
            new DeactivateProductCategoryCommandValidator()
                .Validate(deactivateCommand)
                .IsValid.Should()
                .BeTrue();
            new ReactivateProductCategoryCommandValidator()
                .Validate(reactivateCommand)
                .IsValid.Should()
                .BeTrue();
        }

        [Fact]
        public void Detail_Should_Require_All_Identifiers()
        {
            new GetProductCategoryDetailQueryValidator()
                .Validate(new GetProductCategoryDetailQuery())
                .IsValid.Should()
                .BeFalse();
        }

        [Fact]
        public void List_Should_Reject_Search_That_Exceeds_Maximum_Length()
        {
            var query = new ListProductCategoriesQuery
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Search = new string('a', ProductCategoryConstraints.NormalizedNameMaxLength + 1),
            };
            new ListProductCategoriesQueryValidator().Validate(query).IsValid.Should().BeFalse();
        }
    }
}
