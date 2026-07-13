window.yandexTimesheetDatePicker = {
	initialize: function (
		element,
		initialValue,
		cultureName,
		dotNetReference) {
		if (!element) {
			return;
		}

		if (typeof window.flatpickr !== "function") {
			throw new Error("Flatpickr is not loaded.");
		}

		if (element._flatpickr) {
			element._flatpickr.destroy();
		}

		const useRussianLocale =
			typeof cultureName === "string"
			&& cultureName.toLowerCase().startsWith("ru");

		const locale = useRussianLocale
			? window.flatpickr.l10ns?.ru ?? "default"
			: "default";

		window.flatpickr(element, {
			locale: locale,
			dateFormat: "d.m.Y",
			allowInput: true,
			defaultDate: initialValue || null,

			onChange: function (_, dateString) {
				dotNetReference.invokeMethodAsync(
					"HandleDateChanged",
					dateString || null);
			}
		});
	},

	setValue: function (element, value) {
		if (!element || !element._flatpickr) {
			return;
		}

		element._flatpickr.setDate(value || null, false);
	},

	dispose: function (element) {
		if (!element || !element._flatpickr) {
			return;
		}

		element._flatpickr.destroy();
	}
};