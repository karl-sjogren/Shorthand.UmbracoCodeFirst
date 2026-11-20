using Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.Elements;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class PublishedCompositeModelTests {
    [Fact]
    public void DocumentComposite_ConsumerAndStaticGetterReadPublishedValue() {
        // Arrange
        var content = A.Fake<IPublishedContent>();
        var property = A.Fake<IPublishedProperty>();
        var fallback = A.Fake<IPublishedValueFallback>();
        A.CallTo(() => content.GetProperty("seoCompositeTitle")).Returns(property);
        A.CallTo(() => property.HasValue(null, null)).Returns(true);
        A.CallTo(() => property.GetValue(null, null)).Returns("SEO title");
        var model = new Article(content, fallback);
        ISeoComposite composite = model;

        // Act
        var value = model.SeoCompositeTitle;
        var staticValue = SeoComposite.GetSeoCompositeTitle(composite, fallback);

        // Assert
        value.ShouldBe("SEO title");
        staticValue.ShouldBe("SEO title");
        Article.PropertyAliases.SeoCompositeTitle.ShouldBe("seoCompositeTitle");
        A.CallTo(() => fallback.EnterFallbackScope(A<Fallback>._)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public void ElementComposite_ConsumerAndStaticGetterReadPublishedValue() {
        // Arrange
        var element = A.Fake<IPublishedElement>();
        var property = A.Fake<IPublishedProperty>();
        var fallback = A.Fake<IPublishedValueFallback>();
        A.CallTo(() => element.GetProperty("copyrightYear")).Returns(property);
        A.CallTo(() => property.HasValue(null, null)).Returns(true);
        A.CallTo(() => property.GetValue(null, null)).Returns(2026);
        var model = new CoolElement(element, fallback);
        ICompositeElement composite = model;

        // Act
        var value = model.CopyrightYear;
        var staticValue = CompositeElement.GetCopyrightYear(composite, fallback);

        // Assert
        value.ShouldBe(2026);
        staticValue.ShouldBe(2026);
        CoolElement.PropertyAliases.CopyrightYear.ShouldBe("copyrightYear");
        model.ShouldNotBeAssignableTo<IPublishedContent>();
        A.CallTo(() => fallback.EnterFallbackScope(A<Fallback>._)).MustHaveHappenedTwiceExactly();
    }
}
