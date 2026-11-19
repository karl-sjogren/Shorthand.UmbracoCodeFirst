using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class PublishedElementModelTests {
    [Fact]
    public void GeneratedElement_WrapsElementAndReadsPropertyWithFallback() {
        // Arrange
        var element = A.Fake<IPublishedElement>();
        var property = A.Fake<IPublishedProperty>();
        var fallback = A.Fake<IPublishedValueFallback>();
        A.CallTo(() => element.GetProperty("code")).Returns(property);
        A.CallTo(() => property.HasValue(null, null)).Returns(true);
        A.CallTo(() => property.GetValue(null, null)).Returns("example");

        // Act
        var model = new CodeBlock(element, fallback);
        var value = model.Code;

        // Assert
        element.ShouldNotBeAssignableTo<IPublishedContent>();
        model.ShouldBeAssignableTo<PublishedElementModel>();
        value.ShouldBe("example");
        A.CallTo(() => fallback.EnterFallbackScope(A<Fallback>._)).MustHaveHappenedOnceExactly();
    }
}
