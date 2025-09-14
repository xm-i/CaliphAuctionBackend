namespace CaliphAuctionBackend.Exceptions;

public class ValidationCaliphException(string? message = null, Exception? innerException = null) : CaliphException(message, innerException) {
}