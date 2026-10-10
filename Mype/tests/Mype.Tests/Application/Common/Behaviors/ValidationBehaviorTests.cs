using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Mype.Application.Common.Behaviors;

namespace Mype.Tests.Application.Common.Behaviors
{
    public class ValidationBehaviorTests
    {
        private readonly Mock<IValidator<TestRequest>> _validatorMock = new();

        public sealed class TestRequest
        {
            public string Name { get; set; } = string.Empty;
        }

        public sealed class TestResponse { }

        [Fact]
        public async Task Handle_Should_Execute_Next_When_No_Validators_Exist()
        {
            var behavior = new ValidationBehavior<TestRequest, TestResponse>([]);

            var expectedResponse = new TestResponse();

            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expectedResponse);

            var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task Handle_Should_Execute_Next_When_Request_Is_Valid()
        {
            _validatorMock
                .Setup(validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<TestRequest, TestResponse>([
                _validatorMock.Object,
            ]);

            var expectedResponse = new TestResponse();

            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expectedResponse);

            var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

            result.Should().BeSameAs(expectedResponse);

            _validatorMock.Verify(
                validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Throw_ValidationException_When_Request_Is_Invalid()
        {
            var failures = new List<ValidationFailure>
            {
                new(nameof(TestRequest.Name), "Nombre es obligatorio."),
            };

            _validatorMock
                .Setup(validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new ValidationResult(failures));

            var behavior = new ValidationBehavior<TestRequest, TestResponse>([
                _validatorMock.Object,
            ]);

            var nextExecuted = false;

            RequestHandlerDelegate<TestResponse> next = _ =>
            {
                nextExecuted = true;

                return Task.FromResult(new TestResponse());
            };

            var action = async () =>
                await behavior.Handle(new TestRequest(), next, CancellationToken.None);

            var exception = await action.Should().ThrowAsync<ValidationException>();

            exception
                .Which.Errors.Should()
                .ContainSingle(failure =>
                    failure.PropertyName == nameof(TestRequest.Name)
                    && failure.ErrorMessage == "Nombre es obligatorio."
                );

            nextExecuted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Execute_All_Validators()
        {
            var secondValidatorMock = new Mock<IValidator<TestRequest>>();

            _validatorMock
                .Setup(validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new ValidationResult());

            secondValidatorMock
                .Setup(validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new ValidationResult());

            var behavior = new ValidationBehavior<TestRequest, TestResponse>([
                _validatorMock.Object,
                secondValidatorMock.Object,
            ]);

            RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse());

            await behavior.Handle(new TestRequest(), next, CancellationToken.None);

            _validatorMock.Verify(
                validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            secondValidatorMock.Verify(
                validator =>
                    validator.ValidateAsync(
                        It.IsAny<ValidationContext<TestRequest>>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
