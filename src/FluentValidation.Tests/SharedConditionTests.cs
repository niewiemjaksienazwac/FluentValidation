﻿#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://fluentvalidation.codeplex.com
#endregion

namespace FluentValidation.Tests {
	using System;
	using NUnit.Framework;

	[TestFixture]
	public class SharedConditionTests {
		class SharedConditionValidator : AbstractValidator<Person> {
			public SharedConditionValidator() {
				// Start with a predicate to group rules together.
				// 
				// The AbstractValidator appends this predicate
				// to each inner RuleFor so you only need write,
				// maintain, and think about it in one place.
				//
				// You can finish with an Unless clause that will
				// void the validation for the entire set when it's 
				// predicate is true.
				// 
				When(x => x.Id > 0, () => {
					RuleFor(x => x.Forename).NotEmpty();
					RuleFor(x => x.Surname).NotEmpty().Equal("Smith");
				});
			}
		}

		class SharedConditionWithScopedUnlessValidator : AbstractValidator<Person> {
			public SharedConditionWithScopedUnlessValidator() {
				// inner RuleFor() calls can contain their own,
				// locally scoped When and Unless calls that
				// act only on that individual RuleFor() yet the
				// RuleFor() respects the grouped When() and 
				// Unless() predicates.
				// 
				When(x => x.Id > 0, () => {
					RuleFor(x => x.Orders.Count).Equal(0).Unless(x => String.IsNullOrWhiteSpace(x.CreditCard) == false);
				})
				.Unless(x => x.Age > 65);
			}
		}

		[Test]
		public void Shared_When_is_not_applied_to_grouped_rules_when_initial_predicate_is_false() {
			var validator = new SharedConditionValidator();
			var person = new Person(); // fails the shared When predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Test]
		public void Shared_When_is_applied_to_grouped_rules_when_initial_predicate_is_true() {
			var validator = new SharedConditionValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(3);
		}

		[Test]
		public void Shared_When_is_applied_to_groupd_rules_when_initial_predicate_is_true_and_all_individual_rules_are_satisfied() {
			var validator = new SharedConditionValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Forename = "Kevin", // satisfies RuleFor( x => x.Forename ).NotEmpty()
			                          	Surname = "Smith", // satisfies RuleFor( x => x.Surname ).NotEmpty().Equal( "Smith" )
			                          };

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Test]
		public void Shared_When_respects_the_smaller_scope_of_an_inner_Unless_when_the_inner_Unless_predicate_is_satisfied() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.CreditCard = "1234123412341234"; // satisfies the inner Unless predicate
			person.Orders.Add(new Order());

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}

		[Test]
		public void Shared_When_respects_the_smaller_scope_of_a_inner_Unless_when_the_inner_Unless_predicate_fails() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4 // triggers the shared When predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(1);
		}

		[Test]
		public void Outer_Until_clause_will_trump_an_inner_Until_clause_when_inner_fails_but_the_outer_is_satisfied() {
			var validator = new SharedConditionWithScopedUnlessValidator();
			var person = new Person() {
			                          	Id = 4, // triggers the shared When predicate
			                          	Age = 70 // satisfies the outer Unless predicate
			                          };

			person.Orders.Add(new Order()); // fails the inner Unless predicate

			var result = validator.Validate(person);
			result.Errors.Count.ShouldEqual(0);
		}
	}
}