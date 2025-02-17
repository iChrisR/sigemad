﻿using DGPCE.Sigemad.Application.Contracts.Persistence;
using DGPCE.Sigemad.Domain.Modelos;
using FluentValidation;
using System.Reflection;
using System.Text;

namespace DGPCE.Sigemad.Application.Features.ImpactosEvoluciones.Commands.CreateImpactoEvoluciones;
public class CreateImpactoEvolucionCommandValidator : AbstractValidator<CreateImpactoEvolucionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateImpactoEvolucionCommandValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(command => command.IdImpactoClasificado)
            .NotEmpty().WithMessage("El IdImpactoClasificado es obligatorio.")
            .MustAsync((command, id, cancellation) => ValidarCamposObligatorios(command))
            .WithMessage(command => GenerarMensajeCamposFaltantes(command).Result);
    }

    private async Task<bool> ValidarCamposObligatorios(CreateImpactoEvolucionCommand command)
    {
        var camposFaltantes = await ObtenerCamposFaltantes(command);
        return !camposFaltantes.Any(); // Si no hay campos faltantes, la validación pasa
    }

    private async Task<string> GenerarMensajeCamposFaltantes(CreateImpactoEvolucionCommand command)
    {
        var camposFaltantes = await ObtenerCamposFaltantes(command);
        if (!camposFaltantes.Any())
            return string.Empty;

        // Generar un mensaje con los campos faltantes
        var mensaje = new StringBuilder("Los siguientes campos son obligatorios: ");
        mensaje.Append(string.Join(", ", camposFaltantes));
        return mensaje.ToString();
    }

    private async Task<List<string>> ObtenerCamposFaltantes(CreateImpactoEvolucionCommand command)
    {
        // Obtener los campos obligatorios desde la base de datos
        IReadOnlyList<ValidacionImpactoClasificado> listaCampos = await _unitOfWork.Repository<ValidacionImpactoClasificado>()
            .GetAsync(i => i.IdImpactoClasificado == command.IdImpactoClasificado);

        // Obtener el tipo del comando a validar
        var commandType = command.GetType();

        var camposFaltantes = new List<string>();

        // Validar cada campo requerido
        foreach (var item in listaCampos)
        {
            // Obtener la propiedad correspondiente al campo del comando
            var property = commandType.GetProperty(item.Campo, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                // Si la propiedad no existe en el comando, agregar a la lista de campos faltantes
                camposFaltantes.Add(item.Campo);
                continue;
            }

            // Obtener el valor de la propiedad
            var value = property.GetValue(command);

            // Verificar si el valor es nulo o vacío (en el caso de las cadenas)
            if (value == null || (value is string strValue && string.IsNullOrEmpty(strValue)))
            {
                camposFaltantes.Add(item.Campo); // Agregar a la lista de campos faltantes
            }
        }

        return camposFaltantes;
    }


}
